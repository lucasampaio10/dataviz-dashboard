'use client'

import { useState } from 'react'
import { useDashboard } from '@/hooks/useDashboard'
import { categoryLabel, formatCurrency, formatInteger, formatPercent, formatShortDay } from '@/lib/format'
import { PERIOD_OPTIONS, PeriodDays } from '@/lib/period'
import { ChannelSplit } from './ChannelSplit'
import { RankedBars } from './RankedBars'
import { RecentSales } from './RecentSales'
import { RevenueChart } from './RevenueChart'
import { StatTile } from './StatTile'

export function Dashboard() {
  const [days, setDays] = useState<PeriodDays>(30)
  const [storeId, setStoreId] = useState<string | null>(null)
  const { data, storeOptions, failed, updatedAt } = useDashboard(days, storeId)

  const comparison = `vs. ${days} dias anteriores`

  return (
    <main className="page">
      <header className="masthead">
        <div>
          <h1>Painel de vendas</h1>
          <p>
            {data
              ? `${formatShortDay(data.summary.from)} a ${formatShortDay(data.summary.to)}`
              : 'Carregando o período…'}
          </p>
        </div>

        <div className="filters">
          <div className="segmented" role="group" aria-label="Período">
            {PERIOD_OPTIONS.map((option) => (
              <button key={option} type="button" aria-pressed={option === days} onClick={() => setDays(option)}>
                {option} dias
              </button>
            ))}
          </div>

          <label>
            <span className="sr-only">Loja</span>
            <select className="select" value={storeId ?? ''} onChange={(event) => setStoreId(event.target.value || null)}>
              <option value="">Todas as lojas</option>
              {storeOptions.map((store) => (
                <option key={store.id} value={store.id}>
                  {store.name}
                </option>
              ))}
            </select>
          </label>

          <span className="freshness" data-state={failed ? 'failed' : 'ok'} role="status">
            {failed
              ? 'Sem resposta do servidor; tentando de novo'
              : updatedAt
                ? `Atualizado às ${updatedAt.toLocaleTimeString('pt-BR')}`
                : 'Conectando…'}
          </span>
        </div>
      </header>

      {data && (
        <>
          <section className="headline" aria-label="Indicadores do período">
            <StatTile
              hero
              label="Receita líquida"
              value={formatCurrency(data.summary.netRevenue.value)}
              indicator={data.summary.netRevenue}
              comparison={comparison}
            />
            <StatTile
              label="Pedidos"
              value={formatInteger(data.summary.orders.value)}
              indicator={data.summary.orders}
              comparison={comparison}
            />
            <StatTile
              label="Ticket médio"
              value={formatCurrency(data.summary.averageTicket.value)}
              indicator={data.summary.averageTicket}
              comparison={comparison}
            />
            <StatTile
              label="Taxa de reembolso"
              value={formatPercent(data.summary.refundRate.value)}
              indicator={data.summary.refundRate}
              comparison={comparison}
              lowerIsBetter
            />
          </section>

          <div className="grid">
            <section>
              <h2 className="panel__title">Receita líquida por dia</h2>
              <p className="panel__subtitle">Vendas menos reembolsos, no dia em que a venda aconteceu</p>
              <div className="panel__body">
                <RevenueChart days={data.revenue} />
              </div>
            </section>

            <section>
              <h2 className="panel__title">Receita por canal</h2>
              <p className="panel__subtitle">Onde o cliente fechou a compra</p>
              <div className="panel__body">
                <ChannelSplit channels={data.channels} />
              </div>
            </section>

            <section>
              <h2 className="panel__title">Últimas vendas</h2>
              <p className="panel__subtitle">Todas as lojas, da mais recente para a mais antiga</p>
              <div className="panel__body scroll-x">
                <RecentSales sales={data.recentSales} />
              </div>
            </section>

            <div style={{ display: 'grid', gap: 'var(--space-5)', alignContent: 'start' }}>
              <section>
                <h2 className="panel__title">Receita por categoria</h2>
                <div className="panel__body">
                  <RankedBars slices={data.categories} labelOf={categoryLabel} />
                </div>
              </section>

              {!storeId && (
                <section>
                  <h2 className="panel__title">Receita por loja</h2>
                  <div className="panel__body">
                    <RankedBars slices={data.stores} />
                  </div>
                </section>
              )}
            </div>
          </div>
        </>
      )}

      {!data && failed && <p className="empty">Não foi possível carregar o painel. Verifique se os serviços estão no ar.</p>}
    </main>
  )
}
