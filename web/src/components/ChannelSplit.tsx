import { Slice } from '@/lib/api'
import { channelLabel, formatPercent, formatWholeCurrency } from '@/lib/format'

/**
 * Colour follows the channel, never its rank: a channel keeps its hue when the filter changes
 * and the order of the slices moves.
 */
const SERIES_BY_CHANNEL: Record<string, '1' | '2' | '3'> = {
  InStore: '1',
  Online: '2',
  Marketplace: '3',
}

/** Part-to-whole of three channels: one segmented bar, with the legend carrying the values. */
export function ChannelSplit({ channels }: { channels: Slice[] }) {
  const sold = channels.filter((channel) => channel.revenue > 0)
  if (sold.length === 0) {
    return <p className="empty">Nenhuma venda no período.</p>
  }

  return (
    <>
      <div className="split__bar" aria-hidden="true">
        {sold.map((channel) => (
          <div
            key={channel.label}
            className="split__segment"
            data-series={SERIES_BY_CHANNEL[channel.label] ?? '1'}
            style={{ flexGrow: channel.revenue }}
            title={`${channelLabel(channel.label)}: ${formatPercent(channel.sharePercent)}`}
          />
        ))}
      </div>

      <ul className="split__legend">
        {sold.map((channel) => (
          <li key={channel.label}>
            <span className="split__swatch" data-series={SERIES_BY_CHANNEL[channel.label] ?? '1'} aria-hidden="true" />
            <span>{channelLabel(channel.label)}</span>
            <span className="figure">{formatWholeCurrency(channel.revenue)}</span>
            <span className="split__share figure">{formatPercent(channel.sharePercent)}</span>
          </li>
        ))}
      </ul>
    </>
  )
}
