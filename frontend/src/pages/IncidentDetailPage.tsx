import { useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'

import {
  addIncidentNote,
  assignIncident,
  changeIncidentStatus,
  updateIncident,
  getIncidentById,
  getIncidentHistory,
  getIncidentNotes,
} from '../api/incidents'

import {
  IncidentSeverity,
  IncidentStatus,
  type IncidentDetail,
  type IncidentHistoryItem,
  type IncidentNoteItem,
} from '../types/incidents'

import {
  getNextIncidentStatus,
  getStatusLabel,
} from '../utils/incidents'

interface IncidentDetailPageProps {
  incidentId: string
  onBack: () => void
}

function severityLabel(value: number) {
  switch (value) {
    case IncidentSeverity.Low:
      return 'Baja'
    case IncidentSeverity.Medium:
      return 'Media'
    case IncidentSeverity.High:
      return 'Alta'
    case IncidentSeverity.Critical:
      return 'Crítica'
    default:
      return 'Desconocida'
  }
}

function statusLabel(value: number) {
  switch (value) {
    case IncidentStatus.Open:
      return 'Abierto'
    case IncidentStatus.UnderInvestigation:
      return 'En investigación'
    case IncidentStatus.Contained:
      return 'Contenido'
    case IncidentStatus.Resolved:
      return 'Resuelto'
    case IncidentStatus.Closed:
      return 'Cerrado'
    default:
      return 'Desconocido'
  }
}

function formatDate(value: string | null) {
  if (!value) {
    return '—'
  }

  return new Intl.DateTimeFormat('es-AR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function IncidentDetailPage({
  incidentId,
  onBack,
}: IncidentDetailPageProps) {
  const { session } = useAuth()

  const [incident, setIncident] =
    useState<IncidentDetail | null>(null)

  const [history, setHistory] =
    useState<IncidentHistoryItem[]>([])

  const [notes, setNotes] =
    useState<IncidentNoteItem[]>([])

  const [loading, setLoading] =
    useState(true)

  const [error, setError] =
    useState<string | null>(null)

  const [analystIdentifier, setAnalystIdentifier] =
    useState('')

  const [newStatus, setNewStatus] =
    useState('')

  const [noteContent, setNoteContent] =
    useState('')

  const [editTitle, setEditTitle] =
    useState('')

  const [editDescription, setEditDescription] =
    useState('')

  const [editSeverity, setEditSeverity] =
    useState('')

  const [actionMessage, setActionMessage] =
    useState<string | null>(null)

  const [actionError, setActionError] =
    useState<string | null>(null)

  useEffect(() => {
    const token = session?.accessToken

    setLoading(true)

    Promise.all([
      getIncidentById(incidentId, token),
      getIncidentHistory(incidentId, token),
      getIncidentNotes(incidentId, token),
    ])
      .then(([incidentResult, historyResult, notesResult]) => {
        setIncident(incidentResult)
        setHistory(historyResult)
        setNotes(notesResult)

        setEditTitle(incidentResult.title)
        setEditDescription(incidentResult.description)
        setEditSeverity(String(incidentResult.severity))
        setError(null)
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : 'No se pudo cargar el incidente.',
        )
      })
      .finally(() => {
        setLoading(false)
      })
  }, [incidentId, session])

  async function refreshIncident() {
    const token = session?.accessToken

    const [
      incidentResult,
      historyResult,
      notesResult,
    ] = await Promise.all([
      getIncidentById(incidentId, token),
      getIncidentHistory(incidentId, token),
      getIncidentNotes(incidentId, token),
    ])

    setIncident(incidentResult)
    setHistory(historyResult)
    setNotes(notesResult)
  }

  async function handleUpdateIncident() {
    if (
      !editTitle.trim() ||
      !editDescription.trim() ||
      !editSeverity
    ) {
      setActionError('Completá los datos del incidente.')
      return
    }

    try {
      const token = session?.accessToken

      await updateIncident(
        incidentId,
        {
          title: editTitle.trim(),
          description: editDescription.trim(),
          severity: Number(editSeverity),
        },
        token,
      )

      await refreshIncident()

      setActionError(null)
      setActionMessage('Incidente actualizado correctamente.')
    } catch (requestError: unknown) {
      setActionMessage(null)
      setActionError(
        requestError instanceof Error
          ? requestError.message
          : 'No se pudo actualizar el incidente.',
      )
    }
  }

  async function handleAssign() {
    if (!analystIdentifier.trim()) {
      setActionError('Ingresá un identificador de analista.')
      return
    }

    try {
      const token = session?.accessToken

      await assignIncident(
        incidentId,
        analystIdentifier.trim(),
        token,
      )

      await refreshIncident()

      setActionError(null)
      setActionMessage('Incidente asignado correctamente.')
      setAnalystIdentifier('')
    } catch (requestError: unknown) {
      setActionMessage(null)
      setActionError(
        requestError instanceof Error
          ? requestError.message
          : 'No se pudo asignar el incidente.',
      )
    }
  }

  async function handleStatusChange(
    targetStatus?: number,
  ) {
    const statusToApply =
      targetStatus ?? Number(newStatus)

    if (!statusToApply) {
      setActionError('Seleccioná un estado.')
      return
    }

    try {
      const token = session?.accessToken

      await changeIncidentStatus(
        incidentId,
        statusToApply,
        token,
      )

      await refreshIncident()

      setActionError(null)
      setActionMessage('Estado actualizado correctamente.')
      setNewStatus('')
    } catch (requestError: unknown) {
      setActionMessage(null)
      setActionError(
        requestError instanceof Error
          ? requestError.message
          : 'No se pudo cambiar el estado.',
      )
    }
  }

  async function handleAddNote() {
    if (!noteContent.trim()) {
      setActionError('La nota no puede estar vacía.')
      return
    }

    try {
      const token = session?.accessToken

      await addIncidentNote(
        incidentId,
        noteContent.trim(),
        token,
      )

      await refreshIncident()

      setActionError(null)
      setActionMessage('Nota agregada correctamente.')
      setNoteContent('')
    } catch (requestError: unknown) {
      setActionMessage(null)
      setActionError(
        requestError instanceof Error
          ? requestError.message
          : 'No se pudo agregar la nota.',
      )
    }
  }

  if (loading) {
    return (
      <section className="content-panel">
        <div className="status-message">
          Cargando incidente...
        </div>
      </section>
    )
  }

  if (error || !incident) {
    return (
      <section className="content-panel">
        <button
          type="button"
          className="secondary-button"
          onClick={onBack}
        >
          Volver
        </button>

        <div className="status-message status-error">
          {error ?? 'Incidente no encontrado.'}
        </div>
      </section>
    )
  }

  return (
    <section className="detail-layout">
      <div className="content-panel">
        <div className="detail-toolbar">
          <button
            type="button"
            className="secondary-button"
            onClick={onBack}
          >
            Volver
          </button>

          <span className="incident-id">
            {incident.id}
          </span>
        </div>

        <h2>{incident.title}</h2>

        <p className="incident-description">
          {incident.description}
        </p>

        <div className="detail-grid">
          <div>
            <span className="detail-label">Severidad</span>
            <strong>
              {severityLabel(incident.severity)}
            </strong>
          </div>

          <div>
            <span className="detail-label">Estado</span>
            <strong>
              {statusLabel(incident.status)}
            </strong>
          </div>

          <div>
            <span className="detail-label">Detectado</span>
            <strong>
              {formatDate(incident.detectedAt)}
            </strong>
          </div>

          <div>
            <span className="detail-label">Creado</span>
            <strong>
              {formatDate(incident.createdAt)}
            </strong>
          </div>

          <div>
            <span className="detail-label">Responsable</span>
            <strong>
              {incident.assignedTo ?? 'Sin asignar'}
            </strong>
          </div>

          <div>
            <span className="detail-label">Asignado</span>
            <strong>
              {formatDate(incident.assignedAt)}
            </strong>
          </div>
        </div>
      </div>

      <div className="content-panel">
        <h2>Acciones</h2>

        {actionMessage && (
          <div className="status-message">
            {actionMessage}
          </div>
        )}

        {actionError && (
          <div className="status-message status-error">
            {actionError}
          </div>
        )}

        <div className="edit-incident-box">
          <span className="detail-label">
            Editar incidente
          </span>

          <input
            type="text"
            value={editTitle}
            maxLength={200}
            onChange={(event) =>
              setEditTitle(event.target.value)
            }
          />

          <select
            value={editSeverity}
            onChange={(event) =>
              setEditSeverity(event.target.value)
            }
          >
            <option value={IncidentSeverity.Low}>
              Baja
            </option>
            <option value={IncidentSeverity.Medium}>
              Media
            </option>
            <option value={IncidentSeverity.High}>
              Alta
            </option>
            <option value={IncidentSeverity.Critical}>
              Crítica
            </option>
          </select>

          <textarea
            rows={5}
            maxLength={4000}
            value={editDescription}
            onChange={(event) =>
              setEditDescription(event.target.value)
            }
          />

          <button
            type="button"
            onClick={handleUpdateIncident}
          >
            Guardar cambios
          </button>
        </div>

        <div className="actions-grid">
          <div className="action-box">
            <span className="detail-label">
              Asignar responsable
            </span>

            <input
              type="text"
              placeholder="analista@dominio"
              value={analystIdentifier}
              onChange={(event) =>
                setAnalystIdentifier(event.target.value)
              }
            />

            <button
              type="button"
              onClick={handleAssign}
            >
              Asignar
            </button>
          </div>

          <div className="action-box">
            <span className="detail-label">
              Cambiar estado
            </span>

            {(() => {
              const nextStatus = getNextIncidentStatus(
                incident.status,
              )

              if (nextStatus === null) {
                return (
                  <div className="status-message">
                    El incidente ya se encuentra cerrado.
                  </div>
                )
              }

              return (
                <>
                  <div className="next-status-preview">
                    Próximo estado:
                    <strong>
                      {getStatusLabel(nextStatus)}
                    </strong>
                  </div>

                  <button
                    type="button"
                    onClick={() => {
                      void handleStatusChange(nextStatus)
                    }}
                  >
                    Avanzar estado
                  </button>
                </>
              )
            })()}
          </div>

          <div className="action-box action-box-wide">
            <span className="detail-label">
              Agregar nota
            </span>

            <textarea
              rows={4}
              placeholder="Agregar información de investigación..."
              value={noteContent}
              onChange={(event) =>
                setNoteContent(event.target.value)
              }
            />

            <button
              type="button"
              onClick={handleAddNote}
            >
              Guardar nota
            </button>
          </div>
        </div>
      </div>

      <div className="content-panel">
        <h2>Historial</h2>

        {history.length === 0 ? (
          <div className="status-message">
            No hay eventos registrados.
          </div>
        ) : (
          <div className="timeline">
            {history.map((entry) => (
              <div
                key={entry.id}
                className="timeline-entry"
              >
                <div>
                  <strong>{entry.description}</strong>
                  <span>{entry.performedBy}</span>
                </div>

                <time>
                  {formatDate(entry.occurredAt)}
                </time>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="content-panel">
        <h2>Notas de investigación</h2>

        {notes.length === 0 ? (
          <div className="status-message">
            No hay notas registradas.
          </div>
        ) : (
          <div className="notes-list">
            {notes.map((note) => (
              <article
                key={note.id}
                className="note-entry"
              >
                <p>{note.content}</p>

                <div>
                  <span>{note.createdBy}</span>
                  <time>
                    {formatDate(note.createdAt)}
                  </time>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}
