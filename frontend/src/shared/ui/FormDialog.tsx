import { useLayoutEffect, useRef } from 'react';
import type { ReactNode } from 'react';

/** Modal nativo: foco contenido, Escape y retorno al control que lo abrió. */
export function FormDialog({ title, titleId, busy = false, onDismiss, children }: {
  title: string; titleId: string; busy?: boolean; onDismiss: () => void; children: ReactNode;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const opener = useRef<HTMLElement | null>(null);
  useLayoutEffect(() => {
    const element = dialog.current!;
    opener.current ??= document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (element.showModal) element.showModal();
    else element.setAttribute('open', '');
    const firstField = element.querySelector<HTMLElement>('input:not([readonly]):not([disabled]), select:not([disabled]), textarea:not([disabled])');
    firstField?.focus();
    return () => {
      if (element.close) element.close();
      // StrictMode repite efectos sin desmontar el nodo: no sacar el foco del modal.
      queueMicrotask(() => { if (!element.isConnected && opener.current?.isConnected) opener.current.focus(); });
    };
  }, []);
  useLayoutEffect(() => {
    const element = dialog.current;
    if (!busy && element && !element.contains(document.activeElement)) {
      element.querySelector<HTMLElement>('input:not([readonly]), select, textarea, button')?.focus();
    }
  }, [busy]);
  return <dialog ref={dialog} className="form-dialog" aria-labelledby={titleId} aria-busy={busy}
    onKeyDown={event => {
      if (event.key !== 'Tab') return;
      const controls = [...event.currentTarget.querySelectorAll<HTMLElement>('button, input, select, textarea, a[href], [tabindex]')]
        .filter(control => !control.matches(':disabled') && control.tabIndex >= 0 && control.getClientRects().length > 0);
      const first = controls[0], last = controls.at(-1);
      if (!first) { event.preventDefault(); return; }
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }}
    onCancel={event => { event.preventDefault(); if (!busy) onDismiss(); }}>
    <div className="dialog-heading"><h2 id={titleId}>{title}</h2>
      <button type="button" className="dialog-close" aria-label="Cerrar ventana" disabled={busy} onClick={onDismiss}>×</button>
    </div>
    {children}
  </dialog>;
}
