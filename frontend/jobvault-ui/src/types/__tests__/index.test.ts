import { describe, it, expect } from 'vitest'
import { PIPELINE_STAGES } from '../index'

describe('types', () => {
  describe('PIPELINE_STAGES', () => {
    it('contains all expected stages', () => {
      expect(PIPELINE_STAGES).toContain('Processing')
      expect(PIPELINE_STAGES).toContain('Regenerating')
      expect(PIPELINE_STAGES).toContain('Failed')
      expect(PIPELINE_STAGES).toContain('Not Interested')
      expect(PIPELINE_STAGES).toContain('Archived')
      expect(PIPELINE_STAGES).toContain('Researching')
      expect(PIPELINE_STAGES).toContain('Ready to Apply')
      expect(PIPELINE_STAGES).toContain('Applied')
      expect(PIPELINE_STAGES).toContain('Interview')
      expect(PIPELINE_STAGES).toContain('Offer')
      expect(PIPELINE_STAGES).toContain('Rejected')
    })

    it('has exactly 11 stages', () => {
      expect(PIPELINE_STAGES).toHaveLength(11)
    })

    it('starts with Processing', () => {
      expect(PIPELINE_STAGES[0]).toBe('Processing')
    })

    it('ends with Rejected', () => {
      expect(PIPELINE_STAGES[PIPELINE_STAGES.length - 1]).toBe('Rejected')
    })
  })
})
