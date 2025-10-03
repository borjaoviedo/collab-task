import { describe, it, expect } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Button } from '@shared/ui/Button'
import { FormErrorText } from '@shared/ui/FormErrorText'
import { Checkbox } from '@shared/ui/Checkbox'

describe('UI atomics', () => {
  it('Button respects disabled and isLoading', () => {
    const { rerender } = render(<Button disabled>Click</Button>)
    const btn = screen.getByRole('button', { name: 'Click' })
    expect(btn).toBeDisabled()

    rerender(<Button isLoading>Click</Button>)
    const busy = screen.getByRole('button', { name: 'Click' })
    expect(busy).toHaveAttribute('aria-busy', 'true')
    expect(busy).toBeDisabled()
  })

  it('FormErrorText renders message with alert role', () => {
    render(<FormErrorText>Oops</FormErrorText>)
    expect(screen.getByRole('alert')).toHaveTextContent('Oops')
  })

  it('Checkbox toggles checked state', () => {
    render(<Checkbox aria-label="opt" />)
    const cb = screen.getByRole('checkbox', { name: 'opt' })
    expect(cb).not.toBeChecked()
    fireEvent.click(cb)
    expect(cb).toBeChecked()
  })
})
