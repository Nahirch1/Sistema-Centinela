import { useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'

import {
  getIncidents,
  getIncidentSummary,
} from '../api/incidents'
import { IncidentRow } from '../components/IncidentRow'

import type {
  IncidentListItem,
  IncidentSummary,
} from '../types/incidents'

interface DashboardPageProps {
  onOpenIncident: (id: string) => void
}

export function DashboardPage({
  onOpenIncident,
}: DashboardPageProps) {
  const [incidents, setIncidents] =
    useState<IncidentListItem[]>([])

  const [summary, setSummary] =
    useState<IncidentSummary | null>(null)

  const [loading, setLoading] =
    useState(true)

  const [error, setError] =
    useState<string | null>(null)

  const { session } = useAuth()

  useEffect(() => {
    const token = session?.accessToken

    Promise.all([
      getIncidents(
        {
          pageNumber: 1,
          pageSize: 8,
        },
        token,
      ),
      getIncidentSummary(token),
    ])
      .then(([incidentsResult, summaryResult]) => {
        setIncidents(incidentsResult.items)
        setSummary(summaryResult)
        setError(null)
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : 'Ocurrió un error al consultar la API.',
        )
      })
      .finally(() => {
        setLoading(false)
      })
  }, [session])

  return (
    <>
      <div className="dashboard-note">
        Métricas globales calculadas por el backend sobre todos los incidentes.
      </div>

      <section className="dashboard-grid">
        <div className="dashboard-card">
          <span>Abiertos</span>
          <strong>{summary?.open ?? 0}</strong>
        </div>

        <div className="dashboard-card dashboard-card-critical">
          <span>Críticos</span>
          <strong>{summary?.critical ?? 0}</strong>
        </div>

        <div className="dashboard-card dashboard-card-info">
          <span>En investigación</span>
          <strong>{summary?.underInvestigation ?? 0}</strong>
        </div>

        <div className="dashboard-card dashboard-card-warning">
          <span>Contenidos</span>
          <strong>{summary?.contained ?? 0}</strong>
        </div>

        <div className="dashboard-card dashboard-card-success">
          <span>Resueltos</span>
          <strong>{summary?.resolved ?? 0}</strong>
        </div>
      </section>

      <section className="content-panel">
        <div className="panel-header">
          <div>
            <h2>Incidentes recientes</h2>
            <p>
              Información obtenida directamente desde la API.
            </p>
          </div>
        </div>

        {loading && (
          <div className="status-message">
            Consultando Sistema Centinela...
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
              No hay incidentes registrados.
            </div>
          )}

        {!loading &&
          !error &&
          incidents.length > 0 && (
            <div className="incident-list">
              {incidents.map((incident) => (
                  <IncidentRow
                    key={incident.id}
                    incident={incident}
                    onOpen={onOpenIncident}
                  />
                ))}
            </div>
          )}
      </section>
    </>
  )
}
