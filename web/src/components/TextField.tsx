import { useId, type InputHTMLAttributes } from 'react'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
}

/** A labelled input whose error is tied to it and announced to screen readers (spec 0002, Accessibility). */
export function TextField({ label, error, ...input }: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        aria-invalid={error !== undefined}
        aria-describedby={error === undefined ? undefined : errorId}
        {...input}
      />
      {error !== undefined && (
        <p id={errorId} role="alert" className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
