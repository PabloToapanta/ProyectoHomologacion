export function Brand({ light = false }: { light?: boolean }) {
  return <div className={`brand${light ? ' brand--light' : ''}`}>
    <span className="brand-symbol" aria-hidden="true">
      <svg width="27" height="27" viewBox="0 0 28 28" fill="none"><path d="M4 5.5h7a4 4 0 0 1 3 1.4A4 4 0 0 1 17 5.5h7v17h-7a4 4 0 0 0-3 1.4 4 4 0 0 0-3-1.4H4v-17Z" stroke="currentColor" strokeWidth="1.7" strokeLinejoin="round"/><path d="M14 7v16M8 10h3M17 10h3M8 14h3M17 14h3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>
    </span>
    <span className="brand-name">FISEI <span className="brand-divider">/</span> UTA<span className="brand-caption">Sistema de Homologación</span></span>
  </div>;
}
