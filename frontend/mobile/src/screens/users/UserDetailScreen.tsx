import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { StatusBadge, useUserQuery } from '../../features/users';
import type { UsersStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<UsersStackParamList, 'UserDetail'>;

export function UserDetailScreen({ route, navigation }: Props) {
  const { id } = route.params;
  const { data: user, isPending, isError } = useUserQuery(id);

  if (isPending) {
    return <ActivityIndicator style={styles.spinner} />;
  }

  if (isError || !user) {
    return <Text style={styles.error}>User not found.</Text>;
  }

  return (
    <View style={styles.container}>
      <Text style={styles.name}>
        {user.firstName} {user.lastName}
      </Text>
      <StatusBadge isActive={user.isActive} />

      <View style={styles.field}>
        <Text style={styles.label}>Username</Text>
        <Text>{user.username}</Text>
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Email</Text>
        <Text>{user.email}</Text>
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Phone number</Text>
        <Text>{user.phoneNumber || '—'}</Text>
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Role</Text>
        <Text>{user.roleName}</Text>
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Created</Text>
        <Text>{new Date(user.createdAt).toLocaleString()}</Text>
      </View>

      <Pressable style={styles.editButton} onPress={() => navigation.navigate('UserEdit', { id: user.id })}>
        <Text style={styles.editButtonText}>Edit</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, backgroundColor: '#f7f7f8' },
  spinner: { marginTop: 24 },
  error: { color: '#b3261e', margin: 16 },
  name: { fontSize: 20, fontWeight: '700', marginBottom: 8 },
  field: { marginTop: 16 },
  label: { color: '#666', fontSize: 12, marginBottom: 2 },
  editButton: { marginTop: 24, backgroundColor: '#1f2a44', borderRadius: 6, padding: 14, alignItems: 'center' },
  editButtonText: { color: '#fff', fontWeight: '600' },
});
