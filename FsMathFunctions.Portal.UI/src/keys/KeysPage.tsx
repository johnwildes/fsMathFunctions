import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Title2,
  Button,
  Badge,
  Toolbar,
  ToolbarButton,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  makeStyles,
  tokens,
  Spinner,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components'
import { AddRegular, SignOutRegular, PersonRegular } from '@fluentui/react-icons'
import { api, type ApiKeyDto } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { GenerateKeyModal } from './GenerateKeyModal'

const useStyles = makeStyles({
  root: {
    maxWidth: '900px',
    margin: '0 auto',
    padding: '32px 16px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: '24px',
  },
  headerActions: {
    display: 'flex',
    gap: '8px',
    alignItems: 'center',
  },
  prefix: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    backgroundColor: tokens.colorNeutralBackground3,
    padding: '2px 6px',
    borderRadius: '4px',
  },
})

export function KeysPage() {
  const styles = useStyles()
  const { token, email, role, logout } = useAuth()
  const navigate = useNavigate()

  const [keys, setKeys] = useState<ApiKeyDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showModal, setShowModal] = useState(false)
  const [revoking, setRevoking] = useState<string | null>(null)

  const loadKeys = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.listKeys(token!)
      setKeys(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load keys')
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => { void loadKeys() }, [loadKeys])

  async function handleRevoke(id: string) {
    setRevoking(id)
    try {
      await api.revokeKey(id, token!)
      await loadKeys()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to revoke key')
    } finally {
      setRevoking(null)
    }
  }

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title2>API Keys</Title2>
          <div style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 }}>
            {email}
          </div>
        </div>
        <div className={styles.headerActions}>
          {role === 'Admin' && (
            <Button
              appearance="subtle"
              icon={<PersonRegular />}
              onClick={() => navigate('/admin')}
            >
              Admin
            </Button>
          )}
          <Button appearance="subtle" icon={<SignOutRegular />} onClick={handleLogout}>
            Sign out
          </Button>
        </div>
      </div>

      {error && (
        <MessageBar intent="error" style={{ marginBottom: '16px' }}>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <Toolbar>
        <ToolbarButton
          appearance="primary"
          icon={<AddRegular />}
          onClick={() => setShowModal(true)}
        >
          Generate key
        </ToolbarButton>
      </Toolbar>

      {loading ? (
        <Spinner style={{ marginTop: '48px' }} label="Loading keys…" />
      ) : (
        <Table aria-label="API keys" style={{ marginTop: '16px' }}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Label</TableHeaderCell>
              <TableHeaderCell>Prefix</TableHeaderCell>
              <TableHeaderCell>Created</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell />
            </TableRow>
          </TableHeader>
          <TableBody>
            {keys.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} style={{ textAlign: 'center', color: tokens.colorNeutralForeground3 }}>
                  No API keys yet. Generate one to get started.
                </TableCell>
              </TableRow>
            )}
            {keys.map(k => (
              <TableRow key={k.id}>
                <TableCell>{k.label}</TableCell>
                <TableCell>
                  <span className={styles.prefix}>{k.prefix}…</span>
                </TableCell>
                <TableCell>{new Date(k.createdAt).toLocaleDateString()}</TableCell>
                <TableCell>
                  {k.revokedAt ? (
                    <Badge color="danger" appearance="filled">Revoked</Badge>
                  ) : (
                    <Badge color="success" appearance="filled">Active</Badge>
                  )}
                </TableCell>
                <TableCell>
                  {!k.revokedAt && (
                    <Button
                      appearance="subtle"
                      size="small"
                      disabled={revoking === k.id}
                      onClick={() => handleRevoke(k.id)}
                    >
                      {revoking === k.id ? 'Revoking…' : 'Revoke'}
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <GenerateKeyModal
        open={showModal}
        onClose={() => setShowModal(false)}
        onCreated={loadKeys}
      />
    </div>
  )
}
