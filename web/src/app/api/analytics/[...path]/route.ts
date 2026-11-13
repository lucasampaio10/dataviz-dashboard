import { NextRequest, NextResponse } from 'next/server'

export const dynamic = 'force-dynamic'

const ANALYTICS_API_URL = process.env.ANALYTICS_API_URL ?? 'http://localhost:8082'

/**
 * Same-origin door to the Analytics service. The browser never learns the service address,
 * no CORS is needed, and only dashboard routes can be reached through it.
 */
export async function GET(request: NextRequest, { params }: { params: { path: string[] } }) {
  const path = params.path.map(encodeURIComponent).join('/')
  const target = `${ANALYTICS_API_URL}/api/dashboard/${path}${request.nextUrl.search}`

  try {
    const response = await fetch(target, { cache: 'no-store' })

    return new NextResponse(response.body, {
      status: response.status,
      headers: { 'content-type': response.headers.get('content-type') ?? 'application/json' },
    })
  } catch {
    return NextResponse.json({ title: 'O serviço de análise não respondeu.' }, { status: 502 })
  }
}
