import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import MatchRing from '../MatchRing.vue'

describe('MatchRing', () => {
  it('displays percentage text', () => {
    const wrapper = mount(MatchRing, { props: { pct: 85, recommend: 'Strong Apply' } })
    expect(wrapper.text()).toContain('85%')
  })

  it('displays N/A for null pct', () => {
    const wrapper = mount(MatchRing, { props: { pct: null, recommend: '' } })
    expect(wrapper.text()).toContain('N/A')
  })

  it('renders SVG with circles', () => {
    const wrapper = mount(MatchRing, { props: { pct: 75, recommend: 'Strong Apply' } })
    const circles = wrapper.findAll('circle')
    expect(circles.length).toBe(2)
  })

  it('renders only background circle when pct is null', () => {
    const wrapper = mount(MatchRing, { props: { pct: null, recommend: '' } })
    const circles = wrapper.findAll('circle')
    expect(circles.length).toBe(1)
  })

  it('defaults to 120px size', () => {
    const wrapper = mount(MatchRing, { props: { pct: 50, recommend: 'Moderate Apply' } })
    expect(wrapper.element.style.width).toBe('120px')
    expect(wrapper.element.style.height).toBe('120px')
  })

  it('applies custom size', () => {
    const wrapper = mount(MatchRing, { props: { pct: 50, recommend: 'Moderate Apply', size: 80 } })
    expect(wrapper.element.style.width).toBe('80px')
    expect(wrapper.element.style.height).toBe('80px')
  })

  it('sets correct stroke color from recommend', () => {
    const wrapper = mount(MatchRing, { props: { pct: 90, recommend: 'Strong Apply' } })
    const progressCircle = wrapper.findAll('circle')[1]
    expect(progressCircle.attributes('stroke')).toBe('#22c55e')
  })
})
