import { useLayoutEffect, useRef } from 'react';
import type { ReactNode } from 'react';

/** Modal nativo: foco contenido, Escape y retorno al control que lo abrió. */
export function FormDialog({ title, titleId, busy = false, onDismiss, children }: {
  title: string; titleId: string; busy?: boolean; onDismiss: () => void; children: ReactNode;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  useLayoutEffect(() => {
    const element = dialog.current!;
    const trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (element.showModal) element.showModal();
    else element.setAttribute('open', '');
    return () => {
      if (element.close) element.close();
      queueMicrotask(() => { if (trigger?.isConnected) trigger.focus(); });
    };
  }, []);
  return <dialog ref={dialog} className="form-dialog" aria-labelledby={titleId} aria-busy={busy}
    onCancel={event => { event.preventDefault(); if (!busy) onDismiss(); }}>
    <div className="dialog-heading"><h2 id={titleId}>{title}</h2>
      <button type="button" className="dialog-close" aria-label="Cerrar ventana" disabled={busy} onClick={onDismiss}>×</button>
    </div>
    {children}
  </dialog>;
}
