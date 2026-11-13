'use client'

import { useEffect, useState } from 'react'

export type ChartTheme = {
  series: string
  seriesWash: string
  surface: string
  grid: string
  textPrimary: string
  textMuted: string
  fontFamily: string
}

const FALLBACK: ChartTheme = {
  series: '#2a78d6',
  seriesWash: 'rgba(42, 120, 214, 0.1)',
  surface: '#ffffff',
  grid: '#e9e8e4',
  textPrimary: '#0b0b0b',
  textMuted: '#74736e',
  fontFamily: 'sans-serif',
}

function readTheme(): ChartTheme {
  const styles = getComputedStyle(document.documentElement)
  const token = (name: string, fallback: string) => styles.getPropertyValue(name).trim() || fallback

  return {
    series: token('--series-1', FALLBACK.series),
    seriesWash: token('--series-1-wash', FALLBACK.seriesWash),
    surface: token('--surface-raised', FALLBACK.surface),
    grid: token('--hairline', FALLBACK.grid),
    textPrimary: token('--text-primary', FALLBACK.textPrimary),
    textMuted: token('--text-muted', FALLBACK.textMuted),
    fontFamily: getComputedStyle(document.body).fontFamily,
  }
}

/**
 * A canvas cannot read CSS variables, so the chart receives the resolved design tokens and is
 * redrawn when the colour scheme changes.
 */
export function useChartTheme(): ChartTheme {
  const [theme, setTheme] = useState<ChartTheme>(FALLBACK)

  useEffect(() => {
    const scheme = window.matchMedia('(prefers-color-scheme: dark)')
    const update = () => setTheme(readTheme())

    update()
    scheme.addEventListener('change', update)

    return () => scheme.removeEventListener('change', update)
  }, [])

  return theme
}
