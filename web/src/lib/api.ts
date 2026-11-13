import { Period } from './period'

export type Indicator = {
  value: number
  previous: number
  /** `null` when the previous period has nothing to compare with. */
  changePercent: number | null
}

export type Summary = {
  from: string
  to: string
  previousFrom: string
  previousTo: string
  netRevenue: Indicator
  orders: Indicator
  averageTicket: Indicator
  refundRate: Indicator
}

export type DailyRevenue = { day: string; revenue: number; orders: number }

export type Slice = { label: string; revenue: number; orders: number; sharePercent: number }

export type RecentSale = {
  saleId: string
  storeName: string
  category: string
  channel: string
  amount: number
  occurredAtUtc: string
  refunded: boolean
}

export type StoreOption = { id: string; name: string }

export type DashboardData = {
  summary: Summary
  revenue: DailyRevenue[]
  categories: Slice[]
  channels: Slice[]
  stores: Slice[]
  recentSales: RecentSale[]
}

async function get<T>(path: string, query: URLSearchParams, signal: AbortSignal): Promise<T> {
  const response = await fetch(`/api/analytics/${path}?${query}`, { signal, cache: 'no-store' })
  if (!response.ok) {
    throw new Error(`Analytics respondeu ${response.status} em ${path}`)
  }

  return response.json() as Promise<T>
}

export function fetchStoreOptions(signal: AbortSignal): Promise<StoreOption[]> {
  return get<StoreOption[]>('stores', new URLSearchParams(), signal)
}

export async function fetchDashboard(
  period: Period,
  storeId: string | null,
  signal: AbortSignal,
): Promise<DashboardData> {
  const query = new URLSearchParams({ from: period.from, to: period.to })
  if (storeId) {
    query.set('storeId', storeId)
  }
  const breakdown = (dimension: string) =>
    get<Slice[]>('breakdown', new URLSearchParams({ ...Object.fromEntries(query), dimension }), signal)

  const [summary, revenue, categories, channels, stores, recentSales] = await Promise.all([
    get<Summary>('summary', query, signal),
    get<DailyRevenue[]>('revenue', query, signal),
    breakdown('category'),
    breakdown('channel'),
    breakdown('store'),
    get<RecentSale[]>('recent-sales', new URLSearchParams({ limit: '8' }), signal),
  ])

  return { summary, revenue, categories, channels, stores, recentSales }
}
