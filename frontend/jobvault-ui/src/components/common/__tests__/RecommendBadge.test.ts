import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RecommendBadge from '../RecommendBadge.vue'

describe('RecommendBadge', () => {
  it('renders recommendation text', () => {
    const wrapper = mount(RecommendBadge, { props: { recommend: 'Strong Apply' } })
    expect(wrapper.text()).toContain('Strong Apply')
  })

  it('renders "No report" for empty recommend', () => {
    const wrapper = mount(RecommendBadge, { props: { recommend: '' } })
    expect(wrapper.text()).toContain('No report')
  })

  it('applies emerald colors for Strong Apply', () => {
    const wrapper = mount(RecommendBadge, { props: { recommend: 'Strong Apply' } })
    const span = wrapper.find('span')
    expect(span.classes()).toContain('bg-emerald-500/15')
    expect(span.classes()).toContain('text-emerald-400')
  })

  it('applies amber colors for Moderate Apply', () => {
    const wrapper = mount(RecommendBadge, { props: { recommend: 'Moderate Apply' } })
    const span = wrapper.find('span')
    expect(span.classes()).toContain('bg-amber-500/15')
    expect(span.classes()).toContain('text-amber-400')
  })

  it('applies small size class when size is sm', () => {
    const wrapper = mount(RecommendBadge, { props: { recommend: 'Strong Apply', size: 'sm' } })
    const span = wrapper.find('span')
    expect(span.classes()).toContain('text-[10px]')
  })

  it('applies default size class when size is md', () => {
    const wrapper = mount(RecommendBadge, { props: { recommend: 'Strong Apply', size: 'md' } })
    const span = wrapper.find('span')
    expect(span.classes()).toContain('text-xs')
  })
})
