import { ApiError, type UserProfileFormValues } from '@hms/shared';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ActivityIndicator, ScrollView, Text } from 'react-native';
import { UserForm, useUpdateUserMutation, useUserQuery } from '../../features/users';
import type { UsersStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<UsersStackParamList, 'UserEdit'>;

export function UserEditScreen({ route, navigation }: Props) {
  const { id } = route.params;
  const { data: user, isPending, isError } = useUserQuery(id);
  const mutation = useUpdateUserMutation();

  if (isPending) {
    return <ActivityIndicator style={{ marginTop: 24 }} />;
  }

  if (isError || !user) {
    return <Text style={{ color: '#b3261e', margin: 16 }}>User not found.</Text>;
  }

  function handleSubmit(values: UserProfileFormValues) {
    mutation.mutate(
      {
        id,
        request: {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phoneNumber: values.phoneNumber || undefined,
        },
      },
      {
        onSuccess: () => navigation.navigate('UserDetail', { id }),
      },
    );
  }

  return (
    <ScrollView>
      <UserForm
        submitLabel="Save Changes"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        defaultValues={{
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          phoneNumber: user.phoneNumber ?? '',
        }}
        onSubmit={handleSubmit}
      />
    </ScrollView>
  );
}
