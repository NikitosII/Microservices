import './ServiceCard.css'

function ServiceCard({ name, port, status }) {
  const getStatusColor = () => {
    switch (status) {
      case 'healthy': return '#4CAF50'
      case 'unhealthy': return '#FF9800'
      case 'offline': return '#F44336'
      default: return '#9E9E9E'
    }
  }

  const getStatusIcon = () => {
    switch (status) {
      case 'healthy': return '✅'
      case 'unhealthy': return '⚠️'
      case 'offline': return '❌'
      default: return '⏳'
    }
  }

  const getStatusText = () => {
    switch (status) {
      case 'healthy': return 'Healthy'
      case 'unhealthy': return 'Unhealthy'
      case 'offline': return 'Offline'
      default: return 'Checking...'
    }
  }

  return (
    <div className="service-card" style={{ borderLeftColor: getStatusColor() }}>
      <div className="service-header">
        <h3>{name} API</h3>
        <span className="status-icon">{getStatusIcon()}</span>
      </div>
      <div className="service-details">
        <div className="detail-row">
          <span className="label">Port:</span>
          <span className="value">{port}</span>
        </div>
        <div className="detail-row">
          <span className="label">Status:</span>
          <span className="value" style={{ color: getStatusColor(), fontWeight: 'bold' }}>
            {getStatusText()}
          </span>
        </div>
        <div className="detail-row">
          <span className="label">Endpoint:</span>
          <span className="value endpoint">localhost:{port}</span>
        </div>
      </div>
    </div>
  )
}

export default ServiceCard
