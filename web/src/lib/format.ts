import { BUSINESS_TIME_ZONE } from './period'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const wholeCurrency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 })
const compactCurrency = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
  notation: 'compact',
  maximumFractionDigits: 1,
})
const integer = new Intl.NumberFormat('pt-BR')
const oneDecimal = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
const shortDay = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'short', timeZone: 'UTC' })
const longDay = new Intl.DateTimeFormat('pt-BR', { weekday: 'short', day: '2-digit', month: 'long', timeZone: 'UTC' })
const clock = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit', timeZone: BUSINESS_TIME_ZONE })

export const formatCurrency = (value: number) => currency.format(value)
export const formatWholeCurrency = (value: number) => wholeCurrency.format(value)
export const formatCompactCurrency = (value: number) => compactCurrency.format(value)
export const formatInteger = (value: number) => integer.format(value)
export const formatPercent = (value: number) => `${oneDecimal.format(value)}%`

/** "2026-11-13" → "13 de nov." */
export const formatShortDay = (isoDay: string) => shortDay.format(new Date(`${isoDay}T00:00:00Z`))
export const formatLongDay = (isoDay: string) => longDay.format(new Date(`${isoDay}T00:00:00Z`))
export const formatClock = (isoInstant: string) => clock.format(new Date(isoInstant))

const CATEGORY_LABELS: Record<string, string> = {
  Electronics: 'Eletrônicos',
  Fashion: 'Moda',
  Home: 'Casa',
  Beauty: 'Beleza',
  Sports: 'Esportes',
  Grocery: 'Mercado',
}

const CHANNEL_LABELS: Record<string, string> = {
  InStore: 'Loja física',
  Online: 'Site',
  Marketplace: 'Marketplace',
}

export const categoryLabel = (category: string) => CATEGORY_LABELS[category] ?? category
export const channelLabel = (channel: string) => CHANNEL_LABELS[channel] ?? channel
