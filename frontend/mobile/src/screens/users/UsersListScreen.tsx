import type { User } from '@hms/shared';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import {
  DeleteUserDialog,
  UserListItem,
  useActivateUserMutation,
  useDeactivateUserMutation,
  useDeleteUserMutation,
  useUsersQuery,
} from '../../features/users';
import type { UsersStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<UsersStackParamList, 'UsersList'>;

export function UsersListScreen({ navigation }: Props) {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [userPendingDelete, setUserPendingDelete] = useState<User | null>(null);

  const { data, isPending, isError, error } = useUsersQuery({
    page,
    pageSize: 20,
    sort: '-createdAt',
    search: search || undefined,
  });

  const deleteMutation = useDeleteUserMutation();
  const activateMutation = useActivateUserMutation();
  const deactivateMutation = useDeactivateUserMutation();

  function handleToggleActive(user: User) {
    if (user.isActive) {
      deactivateMutation.mutate(user.id);
    } else {
      activateMutation.mutate(user.id);
    }
  }

  function handleConfirmDelete() {
    if (!userPendingDelete) {
      return;
    }
    deleteMutation.mutate(userPendingDelete.id, { onSuccess: () => setUserPendingDelete(null) });
  }

  return (
    <View style={styles.container}>
      <View style={styles.toolbar}>
        <TextInput
          style={styles.search}
          placeholder="Search by name or email…"
          value={search}
          onChangeText={(text) => {
            setSearch(text);
            setPage(1);
          }}
        />
        <Pressable style={styles.newButton} onPress={() => navigation.navigate('UserCreate')}>
          <Text style={styles.newButtonText}>New</Text>
        </Pressable>
      </View>

      {isPending && <ActivityIndicator style={styles.spinner} />}

      {isError && <Text style={styles.error}>{error instanceof Error ? error.message : 'Failed to load users.'}</Text>}

      {!isPending && !isError && data && data.items.length === 0 && (
        <View style={styles.empty}>
          <Text>No users found{search ? ` for "${search}"` : ''}.</Text>
        </View>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <FlatList
          data={data.items}
          keyExtractor={(user) => user.id}
          renderItem={({ item }) => (
            <UserListItem
              user={item}
              onPress={() => navigation.navigate('UserDetail', { id: item.id })}
              onToggleActive={() => handleToggleActive(item)}
              onDelete={() => setUserPendingDelete(item)}
              isToggling={
                (activateMutation.isPending && activateMutation.variables === item.id) ||
                (deactivateMutation.isPending && deactivateMutation.variables === item.id)
              }
            />
          )}
          ListFooterComponent={
            data.meta.totalPages > 1 ? (
              <View style={styles.pagination}>
                <Pressable disabled={page <= 1} onPress={() => setPage((p) => p - 1)}>
                  <Text>Previous</Text>
                </Pressable>
                <Text>
                  Page {data.meta.page} of {data.meta.totalPages}
                </Text>
                <Pressable disabled={page >= data.meta.totalPages} onPress={() => setPage((p) => p + 1)}>
                  <Text>Next</Text>
                </Pressable>
              </View>
            ) : null
          }
        />
      )}

      {userPendingDelete && (
        <DeleteUserDialog
          user={userPendingDelete}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setUserPendingDelete(null)}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f7f7f8' },
  toolbar: { flexDirection: 'row', gap: 8, padding: 12 },
  search: { flex: 1, borderWidth: 1, borderColor: '#ccc', borderRadius: 6, padding: 8, backgroundColor: '#fff' },
  newButton: { backgroundColor: '#1f2a44', borderRadius: 6, paddingHorizontal: 14, justifyContent: 'center' },
  newButtonText: { color: '#fff', fontWeight: '600' },
  spinner: { marginTop: 24 },
  error: { color: '#b3261e', margin: 12 },
  empty: { padding: 24, alignItems: 'center' },
  pagination: { flexDirection: 'row', justifyContent: 'space-between', padding: 16 },
});
