import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import CompanyAvatar from '../CompanyAvatar.vue'

describe('CompanyAvatar', () => {
  it('renders the first letter uppercased', () => {
    const wrapper = mount(CompanyAvatar, { props: { name: 'acme' } })
    expect(wrapper.text()).toBe('A')
  })

  it('applies a consistent background color for the same name', () => {
    const w1 = mount(CompanyAvatar, { props: { name: 'TestCorp' } })
    const w2 = mount(CompanyAvatar, { props: { name: 'TestCorp' } })
    expect(w1.element.style.backgroundColor).toBe(w2.element.style.backgroundColor)
  })

  it('applies different colors for different names', () => {
    const w1 = mount(CompanyAvatar, { props: { name: 'Acme' } })
    const w2 = mount(CompanyAvatar, { props: { name: 'Zeta' } })
    expect(w1.element.style.backgroundColor).not.toBe(w2.element.style.backgroundColor)
  })

  it('defaults to md size class', () => {
    const wrapper = mount(CompanyAvatar, { props: { name: 'Test' } })
    expect(wrapper.classes()).toContain('w-9')
    expect(wrapper.classes()).toContain('h-9')
  })

  it('applies sm size class', () => {
    const wrapper = mount(CompanyAvatar, { props: { name: 'Test', size: 'sm' } })
    expect(wrapper.classes()).toContain('w-7')
    expect(wrapper.classes()).toContain('h-7')
  })

  it('applies lg size class', () => {
    const wrapper = mount(CompanyAvatar, { props: { name: 'Test', size: 'lg' } })
    expect(wrapper.classes()).toContain('w-11')
    expect(wrapper.classes()).toContain('h-11')
  })

  it('sets a background color from the palette', () => {
    const wrapper = mount(CompanyAvatar, { props: { name: 'Test' } })
    const bg = wrapper.element.style.backgroundColor
    expect(bg).toBeTruthy()
  })
})
