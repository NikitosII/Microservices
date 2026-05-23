/**
 * MetricsCard.jsx — Display card for a single system metric.
 * Props: title (label), value (count or ratio string), icon (emoji), color (accent hex).
 */
import './MetricsCard.css'

function MetricsCard({ title, value, icon, color }) {
  return (
    <div className="metrics-card" style={{ borderTopColor: color }}>
      <div className="metrics-icon" style={{ color: color }}>
        {icon}
      </div>
      <div className="metrics-content">
        <div className="metrics-value">{value}</div>
        <div className="metrics-title">{title}</div>
      </div>
    </div>
  )
}

export default MetricsCard
