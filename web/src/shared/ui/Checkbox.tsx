import type { InputHTMLAttributes } from "react";

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, "type">;

export function Checkbox({ 
    className = "", 
    ...props 
}: Props) {
  return <input type="checkbox" className={["checkbox", className].join(" ")} {...props} />;
}
