import { useId, useState } from 'react';

export default function HelpTip({ label, children }) {
  const id = useId();
  const [open, setOpen] = useState(false);

  return (
    <span className="help-tip"
          onMouseEnter={() => setOpen(true)}
          onMouseLeave={event => {
            if (!event.currentTarget.contains(document.activeElement)) setOpen(false);
          }}
          onBlur={event => {
            if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
          }}
          onKeyDown={event => {
            if (event.key === 'Escape') setOpen(false);
          }}>
      <button type="button" className="help-tip-button" aria-label={label}
              aria-expanded={open} aria-describedby={open ? id : undefined}
              onFocus={() => setOpen(true)} onClick={() => setOpen(true)}>ⓘ</button>
      {open && <span id={id} className="help-tip-content" role="tooltip">{children}</span>}
    </span>
  );
}
