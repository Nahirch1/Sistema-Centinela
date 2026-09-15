import { useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'

import { getIncidents } from '../api/incidents'
import { IncidentRow } from '../components/IncidentRow'

import {
  IncidentSeverity,
  IncidentStatus,
  type IncidentListItem,
} from '../types/incidents'

import {
  getSeverityLabel,
  getStatusLabel,
} from '../utils/incidents'

interface IncidentsPageProps {
  onOpenIncident: (id: string) => void
}

export function IncidentsPage({
  onOpenIncident,
}: IncidentsPageProps) {
  const { session } = useAuth()

  const [incidents, setIncidents] =
    useState<IncidentListItem[]>([])

  const [pageNumber, setPageNumber] =
    useState(1)

  const [totalPages, setTotalPages] =
    useState(0)

  const [searchTerm, setSearchTerm] =
    useState('')

  const [severity, setSeverity] =
    useState('')

  const [status, setStatus] =
    useState('')

  const [assignedTo, setAssignedTo] =
    useState('')

  const [loading, setLoading] =
    useState(true)

  const [error, setError] =
    useState<string | null>(null)

  useEffect(() => {
    const token = session?.accessToken

    setLoading(true)

    getIncidents(
      {
        pageNumber,
        pageSize: 10,
        searchTerm:
          searchTerm.trim() || undefined,
        severity:
          severity === ''
            ? undefined
            : Number(severity),
        status:
          status === ''
            ? undefined
            : Number(status),
        assignedTo:
          assignedTo.trim() || undefined,
      },
      token,
    )
      .then((result) => {
        setIncidents(result.items)
        setTotalPages(result.totalPages)
        setError(null)
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : 'No se pudieron consultar los incidentes.',
        )
      })
      .finally(() => {
        setLoading(false)
      })
  }, [
    session,
    pageNumber,
    searchTerm,
    severity,
    status,
    assignedTo,
  ])

  return (
    <section className="content-panel">
      <div className="panel-header">
        <div>
          <h2>Incidentes</h2>
          <p>
            Consulta y filtrado de incidentes registrados.
          </p>
        </div>
      </div>

      <div className="filters-grid">
        <input
          type="search"
          placeholder="Buscar por título o descripción..."
          value={searchTerm}
          onChange={(event) => {
            setPageNumber(1)
            setSearchTerm(event.target.value)
          }}
        />

        <select
          value={severity}
          onChange={(event) => {
            setPageNumber(1)
            setSeverity(event.target.value)
          }}
        >
          <option value="">Todas las severidades</option>
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

        <select
          value={status}
          onChange={(event) => {
            setPageNumber(1)
            setStatus(event.target.value)
          }}
        >
          <option value="">Todos los estados</option>
          <option value={IncidentStatus.Open}>
            Abierto
          </option>
          <option value={IncidentStatus.UnderInvestigation}>
            En investigación
          </option>
          <option value={IncidentStatus.Contained}>
            Contenido
          </option>
          <option value={IncidentStatus.Resolved}>
            Resuelto
          </option>
          <option value={IncidentStatus.Closed}>
            Cerrado
          </option>
        </select>

        <input
          type="text"
          placeholder="Responsable..."
          value={assignedTo}
          onChange={(event) => {
            setPageNumber(1)
            setAssignedTo(event.target.value)
          }}
        />
      </div>

      {loading && (
        <div className="status-message">
          Consultando incidentes...
        </div>
      )}

      {error && (
        <div className="status-message status-error">
          {error}
        </div>
      )}

      {!loading &&
        !error &&
        incidents.length === 0 && (
          <div className="status-message">
            No se encontraron incidentes.
          </div>
        )}

      {!loading &&
        !error &&
        incidents.length > 0 && (
          <>
            <div className="incident-list">
              {incidents.map((incident) => (
                <div
                  key={incident.id}
                  className="incident-entry"
                >
                  <IncidentRow
                    incident={{
                      ...incident,
                    }}
                    onOpen={onOpenIncident}
                  />

                  <div className="incident-labels">
                    <span>
                      {getSeverityLabel(
                        incident.severity,
                      )}
                    </span>

                    <span>
                      {getStatusLabel(
                        incident.status,
                      )}
                    </span>
                  </div>
                </div>
              ))}
            </div>

            <div className="pagination">
              <button
                type="button"
                disabled={pageNumber <= 1}
                onClick={() =>
                  setPageNumber(
                    (current) =>
                      Math.max(1, current - 1),
                  )
                }
              >
                Anterior
              </button>

              <span>
                Página {pageNumber}
                {totalPages > 0
                  ? ` de ${totalPages}`
                  : ''}
              </span>

              <button
                type="button"
                disabled={
                  totalPages === 0 ||
                  pageNumber >= totalPages
                }
                onClick={() =>
                  setPageNumber(
                    (current) => current + 1,
                  )
                }
              >
                Siguiente
              </button>
            </div>
          </>
        )}
    </section>
  )
}
