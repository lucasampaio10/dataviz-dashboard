import { Slice } from '@/lib/api'
import { formatInteger, formatPercent, formatWholeCurrency } from '@/lib/format'

type RankedBarsProps = {
  slices: Slice[]
  labelOf?: (label: string) => string
}

/**
 * Magnitude comparison: one hue, sorted, every bar on the same baseline. The value sits beside
 * its label, so nothing depends on hovering or on telling colours apart.
 */
export function RankedBars({ slices, labelOf = (label) => label }: RankedBarsProps) {
  if (slices.length === 0) {
    return <p className="empty">Nenhuma venda no período.</p>
  }

  const largest = Math.max(...slices.map((slice) => slice.revenue), 1)

  return (
    <ol className="ranked">
      {slices.map((slice) => (
        <li key={slice.label} className="ranked__row">
          <span className="ranked__label">{labelOf(slice.label)}</span>
          <span className="ranked__value figure">{formatWholeCurrency(slice.revenue)}</span>
          <div className="ranked__track" aria-hidden="true">
            <div className="ranked__bar" style={{ width: `${(Math.max(slice.revenue, 0) / largest) * 100}%` }} />
          </div>
          <span className="ranked__meta">
            {formatPercent(slice.sharePercent)} da receita · {formatInteger(slice.orders)} pedidos
          </span>
        </li>
      ))}
    </ol>
  )
}
