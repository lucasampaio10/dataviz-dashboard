/** @type {import('next').NextConfig} */
const nextConfig = {
  // Self-contained server bundle: the production image ships without node_modules.
  output: 'standalone',
  reactStrictMode: true,
}

export default nextConfig
