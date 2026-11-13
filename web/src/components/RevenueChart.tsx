'use client'

import {
  CategoryScale,
  Chart as ChartJS,
  ChartData,
  ChartOptions,
  Filler,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
} from 'chart.js'
import { useMemo } from 'react'
import { Line } from 'react-chartjs-2'
import { ChartTheme, useChartTheme } from '@/hooks/useChartTheme'
import { DailyRevenue } from '@/lib/api'
import { formatCompactCurrency, formatCurrency, formatInteger, formatLongDay, formatShortDay } from '@/lib/format'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Filler, Tooltip)

export function RevenueChart({ days }: { days: DailyRevenue[] }) {
  const theme = useChartTheme()
  const data = useMemo(() => toChartData(days, theme), [days, theme])
  const options = useMemo(() => toChartOptions(days, theme), [days, theme])

  return (
    <>
      <div className="chart">
        <Line data={data} options={options} aria-label="Gráfico de linha da receita líquida por dia" role="img" />
      </div>

      <table className="sr-only">
        <caption>Receita líquida e pedidos por dia</caption>
        <thead>
          <tr>
            <th scope="col">Dia</th>
            <th scope="col">Receita líquida</th>
            <th scope="col">Pedidos</th>
          </tr>
        </thead>
        <tbody>
          {days.map((day) => (
            <tr key={day.day}>
              <th scope="row">{formatLongDay(day.day)}</th>
              <td>{formatCurrency(day.revenue)}</td>
              <td>{formatInteger(day.orders)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  )
}

function toChartData(days: DailyRevenue[], theme: ChartTheme): ChartData<'line'> {
  return {
    labels: days.map((day) => formatShortDay(day.day)),
    datasets: [
      {
        data: days.map((day) => day.revenue),
        borderColor: theme.series,
        borderWidth: 2,
        borderJoinStyle: 'round',
        borderCapStyle: 'round',
        backgroundColor: theme.seriesWash,
        fill: true,
        tension: 0,
        pointRadius: 0,
        pointHoverRadius: 5,
        pointHoverBackgroundColor: theme.series,
        pointHoverBorderColor: theme.surface,
        pointHoverBorderWidth: 2,
      },
    ],
  }
}

function toChartOptions(days: DailyRevenue[], theme: ChartTheme): ChartOptions<'line'> {
  const font = { family: theme.fontFamily, size: 12 }

  return {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    // The whole column under the cursor is the hit target, not the 2px line.
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { display: false },
      tooltip: {
        backgroundColor: theme.textPrimary,
        titleColor: theme.surface,
        bodyColor: theme.surface,
        titleFont: { ...font, weight: 'normal' },
        bodyFont: { ...font, weight: 'bold' },
        padding: 10,
        cornerRadius: 4,
        displayColors: false,
        callbacks: {
          title: (items) => {
            const day = days[items[0]?.dataIndex ?? 0]
            return day ? formatLongDay(day.day) : ''
          },
          label: (item) => {
            const day = days[item.dataIndex]
            return day ? `${formatCurrency(day.revenue)} em ${formatInteger(day.orders)} pedidos` : ''
          },
        },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        border: { color: theme.grid },
        ticks: { color: theme.textMuted, font, maxRotation: 0, autoSkipPadding: 24 },
      },
      y: {
        beginAtZero: true,
        grid: { color: theme.grid, lineWidth: 1 },
        border: { display: false },
        ticks: {
          color: theme.textMuted,
          font,
          maxTicksLimit: 5,
          callback: (value) => formatCompactCurrency(Number(value)),
        },
      },
    },
  }
}
