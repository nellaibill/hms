import type { User } from '@hms/shared';
import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';

interface DeleteUserDialogProps {
  user: User;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteUserDialog({ user, isDeleting, onConfirm, onCancel }: DeleteUserDialogProps) {
  return (
    <Modal transparent animationType="fade" visible onRequestClose={onCancel}>
      <View style={styles.backdrop}>
        <View style={styles.dialog}>
          <Text style={styles.title}>Delete user?</Text>
          <Text style={styles.body}>
            This will remove {user.firstName} {user.lastName} ({user.email}) from active lists. The record is
            retained (soft delete).
          </Text>
          <View style={styles.actions}>
            <Pressable onPress={onCancel} disabled={isDeleting} style={styles.button}>
              <Text>Cancel</Text>
            </Pressable>
            <Pressable onPress={onConfirm} disabled={isDeleting} style={styles.button}>
              <Text style={styles.deleteText}>{isDeleting ? 'Deleting…' : 'Delete'}</Text>
            </Pressable>
          </View>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: { flex: 1, backgroundColor: 'rgba(0,0,0,0.4)', justifyContent: 'center', padding: 24 },
  dialog: { backgroundColor: '#fff', borderRadius: 8, padding: 20 },
  title: { fontWeight: '700', fontSize: 16, marginBottom: 8 },
  body: { color: '#333', marginBottom: 16 },
  actions: { flexDirection: 'row', justifyContent: 'flex-end', gap: 16 },
  button: { paddingVertical: 6, paddingHorizontal: 10 },
  deleteText: { color: '#b3261e', fontWeight: '600' },
});
