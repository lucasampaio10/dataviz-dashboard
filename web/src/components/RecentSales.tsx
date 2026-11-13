import { RecentSale } from '@/lib/api'
import { categoryLabel, channelLabel, formatClock, formatCurrency } from '@/lib/format'

export function RecentSales({ sales }: { sales: RecentSale[] }) {
  if (sales.length === 0) {
    return <p className="empty">Nenhuma venda registrada ainda.</p>
  }

  return (
    <table className="sales">
      <thead>
        <tr>
          <th scope="col">Hora</th>
          <th scope="col">Loja</th>
          <th scope="col">Categoria</th>
          <th scope="col">Canal</th>
          <th scope="col" className="numeric">
            Valor
          </th>
        </tr>
      </thead>
      <tbody>
        {sales.map((sale) => (
          <tr key={sale.saleId} data-refunded={sale.refunded}>
            <td className="figure">{formatClock(sale.occurredAtUtc)}</td>
            <td>{sale.storeName}</td>
            <td>{categoryLabel(sale.category)}</td>
            <td>{channelLabel(sale.channel)}</td>
            <td className="numeric">
              <span className="amount figure">{formatCurrency(sale.amount)}</span>
              {sale.refunded && <span className="tag">Reembolsada</span>}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
