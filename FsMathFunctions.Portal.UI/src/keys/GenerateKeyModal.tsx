import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Input,
  Label,
  Button,
  MessageBar,
  MessageBarBody,
  makeStyles,
} from '@fluentui/react-components'
import { api, type CreateKeyResponse } from '../api/client'
import { useAuth } from '../auth/AuthContext'

const useStyles = makeStyles({
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    marginBottom: '16px',
  },
  rawKey: {
    fontFamily: 'monospace',
    padding: '8px',
    backgroundColor: '#f5f5f5',
    borderRadius: '4px',
    wordBreak: 'break-all',
    userSelect: 'all',
  },
})

interface Props {
  open: boolean
  onClose: () => void
  onCreated: () => void
}

export function GenerateKeyModal({ open, onClose, onCreated }: Props) {
  const styles = useStyles()
  const { token } = useAuth()

  const [label, setLabel] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [created, setCreated] = useState<CreateKeyResponse | null>(null)

  // Reset internal state each time the modal opens so stale state never shows.
  useEffect(() => {
    if (open) {
      setLabel('')
      setError(null)
      setCreated(null)
    }
  }, [open])

  function handleClose() {
    const wasCreated = created !== null
    onClose()
    if (wasCreated) {
      onCreated()
    }
  }

  async function handleCreate() {
    if (!label.trim()) {
      setError('Label is required')
      return
    }
    setError(null)
    setLoading(true)
    try {
      const result = await api.createKey({ label: label.trim() }, token!)
      setCreated(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create key')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, d) => { if (!d.open) handleClose() }}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Generate API Key</DialogTitle>
          <DialogContent>
            {created ? (
              <>
                <MessageBar intent="success">
                  <MessageBarBody>
                    Key created. Copy it now — it will not be shown again.
                  </MessageBarBody>
                </MessageBar>
                <div style={{ marginTop: '12px' }}>
                  <Label>Your API Key</Label>
                  <div className={styles.rawKey}>{created.rawKey}</div>
                </div>
              </>
            ) : (
              <>
                {error && (
                  <MessageBar intent="error" style={{ marginBottom: '12px' }}>
                    <MessageBarBody>{error}</MessageBarBody>
                  </MessageBar>
                )}
                <div className={styles.field}>
                  <Label htmlFor="key-label">Label</Label>
                  <Input
                    id="key-label"
                    placeholder="e.g. Production server"
                    value={label}
                    onChange={(_, d) => setLabel(d.value)}
                    autoFocus
                  />
                </div>
              </>
            )}
          </DialogContent>
          <DialogActions>
            {created ? (
              <Button appearance="primary" onClick={handleClose}>Done</Button>
            ) : (
              <>
                <Button onClick={handleClose}>Cancel</Button>
                <Button appearance="primary" onClick={handleCreate} disabled={loading}>
                  {loading ? 'Creating…' : 'Create'}
                </Button>
              </>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
