/** Time zone that defines the business day; must match the Analytics service. */
export const BUSINESS_TIME_ZONE = 'America/Fortaleza'

export const PERIOD_OPTIONS = [7, 30, 90] as const

export type PeriodDays = (typeof PERIOD_OPTIONS)[number]

export type Period = { from: string; to: string; days: PeriodDays }

const isoDay = new Intl.DateTimeFormat('en-CA', {
  timeZone: BUSINESS_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

/** The last `days` business days, ending today. */
export function lastDays(days: PeriodDays, now: Date = new Date()): Period {
  const to = isoDay.format(now)
  const start = new Date(`${to}T12:00:00Z`)
  start.setUTCDate(start.getUTCDate() - (days - 1))

  return { from: start.toISOString().slice(0, 10), to, days }
}
