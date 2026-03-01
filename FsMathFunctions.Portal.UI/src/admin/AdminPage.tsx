import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Title2,
  Button,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  makeStyles,
  tokens,
  Spinner,
  MessageBar,
  MessageBarBody,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
} from '@fluentui/react-components'
import { ArrowLeftRegular } from '@fluentui/react-icons'
import { api, type UserSummaryDto } from '../api/client'
import { useAuth } from '../auth/AuthContext'

const useStyles = makeStyles({
  root: {
    maxWidth: '900px',
    margin: '0 auto',
    padding: '32px 16px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    marginBottom: '24px',
  },
})

export function AdminPage() {
  const styles = useStyles()
  const { token } = useAuth()
  const navigate = useNavigate()

  const [users, setUsers] = useState<UserSummaryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<UserSummaryDto | null>(null)

  const loadUsers = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.listUsers(token!)
      setUsers(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load users')
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => { void loadUsers() }, [loadUsers])

  async function handleDelete(user: UserSummaryDto) {
    setDeleting(user.id)
    setConfirmDelete(null)
    try {
      await api.deleteUser(user.id, token!)
      await loadUsers()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete user')
    } finally {
      setDeleting(null)
    }
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Button
          appearance="subtle"
          icon={<ArrowLeftRegular />}
          onClick={() => navigate('/keys')}
        >
          Back
        </Button>
        <Title2>User Administration</Title2>
      </div>

      {error && (
        <MessageBar intent="error" style={{ marginBottom: '16px' }}>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {loading ? (
        <Spinner label="Loading users…" style={{ marginTop: '48px' }} />
      ) : (
        <Table aria-label="Users">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Email</TableHeaderCell>
              <TableHeaderCell>Role</TableHeaderCell>
              <TableHeaderCell>Keys</TableHeaderCell>
              <TableHeaderCell>Registered</TableHeaderCell>
              <TableHeaderCell />
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map(u => (
              <TableRow key={u.id}>
                <TableCell>{u.email}</TableCell>
                <TableCell>
                  <Badge
                    color={u.role === 'Admin' ? 'warning' : 'informative'}
                    appearance="filled"
                  >
                    {u.role}
                  </Badge>
                </TableCell>
                <TableCell>{u.keyCount}</TableCell>
                <TableCell>{new Date(u.createdAt).toLocaleDateString()}</TableCell>
                <TableCell>
                  <Button
                    appearance="subtle"
                    size="small"
                    disabled={deleting === u.id}
                    onClick={() => setConfirmDelete(u)}
                  >
                    {deleting === u.id ? 'Deleting…' : 'Delete'}
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Dialog
        open={confirmDelete !== null}
        onOpenChange={(_, d) => { if (!d.open) setConfirmDelete(null) }}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Delete user</DialogTitle>
            <DialogContent>
              Are you sure you want to delete <strong>{confirmDelete?.email}</strong>?
              This will also delete all their API keys and cannot be undone.
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setConfirmDelete(null)}>Cancel</Button>
              <Button
                appearance="primary"
                style={{ backgroundColor: tokens.colorPaletteRedBackground3 }}
                onClick={() => { if (confirmDelete) void handleDelete(confirmDelete) }}
              >
                Delete
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
