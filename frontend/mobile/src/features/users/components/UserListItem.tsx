import type { User } from '@hms/shared';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { StatusBadge } from './StatusBadge';

interface UserListItemProps {
  user: User;
  onPress: () => void;
  onToggleActive: () => void;
  onDelete: () => void;
  isToggling: boolean;
}

export function UserListItem({ user, onPress, onToggleActive, onDelete, isToggling }: UserListItemProps) {
  return (
    <Pressable style={styles.row} onPress={onPress}>
      <View style={styles.info}>
        <Text style={styles.name}>
          {user.firstName} {user.lastName}
        </Text>
        <Text style={styles.email}>@{user.username}</Text>
        <Text style={styles.email}>{user.email}</Text>
        <Text style={styles.email}>{user.roleName}</Text>
        <StatusBadge isActive={user.isActive} />
      </View>
      <View style={styles.actions}>
        <Pressable onPress={onToggleActive} disabled={isToggling} style={styles.actionButton}>
          <Text style={styles.actionText}>{user.isActive ? 'Deactivate' : 'Activate'}</Text>
        </Pressable>
        <Pressable onPress={onDelete} style={styles.actionButton}>
          <Text style={[styles.actionText, styles.deleteText]}>Delete</Text>
        </Pressable>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    padding: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#e5e5e5',
    backgroundColor: '#fff',
  },
  info: { flex: 1, gap: 4 },
  name: { fontWeight: '600' },
  email: { color: '#555', fontSize: 13 },
  actions: { justifyContent: 'center', gap: 8 },
  actionButton: { paddingVertical: 4 },
  actionText: { color: '#1f2a44', fontSize: 13 },
  deleteText: { color: '#b3261e' },
});
