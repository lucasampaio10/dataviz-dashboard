import { Indicator } from '@/lib/api'
import { formatPercent } from '@/lib/format'

type StatTileProps = {
  label: string
  value: string
  indicator: Indicator
  comparison: string
  hero?: boolean
  /** For indicators such as refund rate, where going up is bad news. */
  lowerIsBetter?: boolean
}

export function StatTile({ label, value, indicator, comparison, hero = false, lowerIsBetter = false }: StatTileProps) {
  const change = indicator.changePercent

  return (
    <div className={hero ? 'stat stat--hero' : 'stat'}>
      <span className="stat__label">{label}</span>
      <span className="stat__value figure">{value}</span>
      {change === null ? (
        <p className="stat__delta">Sem período anterior para comparar</p>
      ) : (
        <p className="stat__delta" data-tone={toneOf(change, lowerIsBetter)}>
          <strong className="figure">
            {arrowOf(change)} {formatPercent(Math.abs(change))}
          </strong>{' '}
          {comparison}
        </p>
      )}
    </div>
  )
}

function arrowOf(change: number): string {
  if (change === 0) {
    return '='
  }

  return change > 0 ? '▲' : '▼'
}

function toneOf(change: number, lowerIsBetter: boolean): 'good' | 'bad' | 'neutral' {
  if (change === 0) {
    return 'neutral'
  }

  return change > 0 !== lowerIsBetter ? 'good' : 'bad'
}
