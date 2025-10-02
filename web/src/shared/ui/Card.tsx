import { type ReactNode, type HTMLAttributes, useId } from "react";

type CardProps = HTMLAttributes<HTMLElement> & {
  title?: string;
  header?: ReactNode;
  footer?: ReactNode;
  children: ReactNode;
  className?: string;
};

export function Card({ title, header, footer, children, className = "", ...props }: CardProps) {
  const headingId = useId();
  const hasTitle = Boolean(title);
  return (
    <section
      role="region"
      aria-labelledby={hasTitle ? headingId : undefined}
      className={["card", className].filter(Boolean).join(" ")}
      {...props}
    >
      {header}
      {hasTitle && <h2 id={headingId} className="card-title">{title}</h2>}
      <div className="card-body">{children}</div>
      {footer}
    </section>
  );
}
