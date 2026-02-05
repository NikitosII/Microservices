import { useState, useEffect } from 'react'
import './App.css'
import ServiceCard from './components/ServiceCard'
import MetricsCard from './components/MetricsCard'

function App() {
  const [services, setServices] = useState([
    { name: 'Gateway', url: 'http://localhost:5000/api/health', port: 5000, status: 'checking' },
    { name: 'Identity', url: 'http://localhost:5001/api/health', port: 5001, status: 'checking' },
    { name: 'Product', url: 'http://localhost:5002/api/health', port: 5002, status: 'checking' },
    { name: 'Coupon', url: 'http://localhost:5003/api/health', port: 5003, status: 'checking' },
    { name: 'Shopping Cart', url: 'http://localhost:5004/api/health', port: 5004, status: 'checking' },
    { name: 'Order', url: 'http://localhost:5005/api/health', port: 5005, status: 'checking' },
    { name: 'Payment', url: 'http://localhost:5007/api/health', port: 5007, status: 'checking' },
  ])

  const [metrics, setMetrics] = useState({
    products: 0,
    orders: 0,
    coupons: 0
  })

  const [lastUpdate, setLastUpdate] = useState(new Date())

  const checkServiceHealth = async (service) => {
    try {
      const response = await fetch(service.url, {
        method: 'GET',
        headers: { 'Accept': 'application/json' }
      })
      return response.ok ? 'healthy' : 'unhealthy'
    } catch (error) {
      return 'offline'
    }
  }

  const fetchMetrics = async () => {
    try {
      // Fetch products count
      const productsRes = await fetch('http://localhost:5002/api/products')
      if (productsRes.ok) {
        const products = await productsRes.json()
        setMetrics(prev => ({ ...prev, products: Array.isArray(products) ? products.length : 0 }))
      }
    } catch (error) {
      console.error('Error fetching products:', error)
    }

    try {
      // Fetch orders count
      const ordersRes = await fetch('http://localhost:5005/api/orders')
      if (ordersRes.ok) {
        const orders = await ordersRes.json()
        setMetrics(prev => ({ ...prev, orders: Array.isArray(orders) ? orders.length : 0 }))
      }
    } catch (error) {
      console.error('Error fetching orders:', error)
    }

    try {
      // Fetch coupons count
      const couponsRes = await fetch('http://localhost:5003/api/coupons')
      if (couponsRes.ok) {
        const coupons = await couponsRes.json()
        setMetrics(prev => ({ ...prev, coupons: Array.isArray(coupons) ? coupons.length : 0 }))
      }
    } catch (error) {
      console.error('Error fetching coupons:', error)
    }
  }

  const checkAllServices = async () => {
    const updatedServices = await Promise.all(
      services.map(async (service) => ({
        ...service,
        status: await checkServiceHealth(service)
      }))
    )
    setServices(updatedServices)
    setLastUpdate(new Date())
  }

  useEffect(() => {
    checkAllServices()
    fetchMetrics()

    // Auto-refresh every 10 seconds
    const interval = setInterval(() => {
      checkAllServices()
      fetchMetrics()
    }, 10000)

    return () => clearInterval(interval)
  }, [])

  const healthyCount = services.filter(s => s.status === 'healthy').length
  const totalCount = services.length

  return (
    <div className="app">
      <header className="header">
        <h1> Microservices Dashboard</h1>
        <div className="header-info">
          <div className="status-badge">
            <span className={`badge ${healthyCount === totalCount ? 'healthy' : 'warning'}`}>
              {healthyCount}/{totalCount} Services Online
            </span>
          </div>
          <div className="last-update">
            Last updated: {lastUpdate.toLocaleTimeString()}
          </div>
        </div>
      </header>

      <main className="main-content">
        <section className="metrics-section">
          <h2>System Metrics</h2>
          <div className="metrics-grid">
            <MetricsCard
              title="Products"
              value={metrics.products}
              icon="📦"
              color="#4CAF50"
            />
            <MetricsCard
              title="Orders"
              value={metrics.orders}
              icon="🛒"
              color="#2196F3"
            />
            <MetricsCard
              title="Coupons"
              value={metrics.coupons}
              icon="🎟️"
              color="#FF9800"
            />
            <MetricsCard
              title="Services"
              value={`${healthyCount}/${totalCount}`}
              icon="⚙️"
              color="#9C27B0"
            />
          </div>
        </section>

        <section className="services-section">
          <h2>Service Health Status</h2>
          <div className="services-grid">
            {services.map((service) => (
              <ServiceCard
                key={service.name}
                name={service.name}
                port={service.port}
                status={service.status}
              />
            ))}
          </div>
        </section>

        <section className="actions-section">
          <button className="refresh-btn" onClick={() => {
            checkAllServices()
            fetchMetrics()
          }}>
            🔄 Refresh Now
          </button>
        </section>
      </main>

      <footer className="footer">
        <p>E-Commerce Microservices Platform | Auto-refresh every 10 seconds</p>
      </footer>
    </div>
  )
}

export default App
