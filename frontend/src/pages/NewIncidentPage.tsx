import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'

import { createIncident } from '../api/incidents'
import { IncidentSeverity } from '../types/incidents'

interface NewIncidentPageProps {
  onCreated: (id: string) => void
}

export function NewIncidentPage({
  onCreated,
}: NewIncidentPageProps) {
  const { session } = useAuth()

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [severity, setSeverity] = useState(
    String(IncidentSeverity.Medium),
  )
  const [detectedAt, setDetectedAt] = useState('')

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (
      !title.trim() ||
      !description.trim() ||
      !detectedAt
    ) {
      setError('Completá todos los campos obligatorios.')
      return
    }

    try {
      setSaving(true)
      setError(null)

      const token = session?.accessToken

      const result = await createIncident(
        {
          title: title.trim(),
          description: description.trim(),
          severity: Number(severity),
          detectedAt: new Date(detectedAt).toISOString(),
        },
        token,
      )

      onCreated(result.id)
    } catch (requestError: unknown) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : 'No se pudo crear el incidente.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="content-panel">
      <div className="panel-header">
        <div>
          <h2>Nuevo incidente</h2>
          <p>
            Registrar un nuevo evento de seguridad.
          </p>
        </div>
      </div>

      {error && (
        <div className="status-message status-error">
          {error}
        </div>
      )}

      <form
        className="incident-form"
        onSubmit={handleSubmit}
      >
        <label>
          <span>Título</span>
          <input
            type="text"
            maxLength={200}
            value={title}
            onChange={(event) =>
              setTitle(event.target.value)
            }
            placeholder="Ej. Ejecución sospechosa de PowerShell"
          />
        </label>

        <label>
          <span>Severidad</span>
          <select
            value={severity}
            onChange={(event) =>
              setSeverity(event.target.value)
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
        </label>

        <label>
          <span>Fecha de detección</span>
          <input
            type="datetime-local"
            value={detectedAt}
            onChange={(event) =>
              setDetectedAt(event.target.value)
            }
          />
        </label>

        <label className="form-field-wide">
          <span>Descripción</span>
          <textarea
            rows={8}
            maxLength={4000}
            value={description}
            onChange={(event) =>
              setDescription(event.target.value)
            }
            placeholder="Describí el evento detectado..."
          />
        </label>

        <div className="form-actions form-field-wide">
          <button
            type="submit"
            disabled={saving}
          >
            {saving
              ? 'Registrando...'
              : 'Registrar incidente'}
          </button>
        </div>
      </form>
    </section>
  )
}
