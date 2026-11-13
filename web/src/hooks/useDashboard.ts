'use client'

import { useEffect, useState } from 'react'
import { DashboardData, StoreOption, fetchDashboard, fetchStoreOptions } from '@/lib/api'
import { PeriodDays, lastDays } from '@/lib/period'

const REFRESH_INTERVAL_MS = 5_000

export type DashboardState = {
  data: DashboardData | null
  storeOptions: StoreOption[]
  /** The last refresh failed; `data` may still hold the previous good answer. */
  failed: boolean
  updatedAt: Date | null
}

/**
 * Loads the dashboard and keeps it fresh. The read model is eventually consistent, so polling
 * is what makes a sale recorded a moment ago show up without a reload.
 */
export function useDashboard(days: PeriodDays, storeId: string | null): DashboardState {
  const [state, setState] = useState<DashboardState>({ data: null, storeOptions: [], failed: false, updatedAt: null })

  useEffect(() => {
    const controller = new AbortController()

    async function refresh() {
      try {
        const [data, storeOptions] = await Promise.all([
          fetchDashboard(lastDays(days), storeId, controller.signal),
          fetchStoreOptions(controller.signal),
        ])
        setState({ data, storeOptions, failed: false, updatedAt: new Date() })
      } catch (error) {
        if (!controller.signal.aborted) {
          setState((previous) => ({ ...previous, failed: true }))
        }
      }
    }

    void refresh()
    const timer = setInterval(() => void refresh(), REFRESH_INTERVAL_MS)

    return () => {
      controller.abort()
      clearInterval(timer)
    }
  }, [days, storeId])

  return state
}
