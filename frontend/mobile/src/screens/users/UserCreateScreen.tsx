import { ApiError, type UserProfileFormValues } from '@hms/shared';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ScrollView } from 'react-native';
import { UserForm, useCreateUserMutation } from '../../features/users';
import type { UsersStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<UsersStackParamList, 'UserCreate'>;

export function UserCreateScreen({ navigation }: Props) {
  const mutation = useCreateUserMutation();

  function handleSubmit(values: UserProfileFormValues) {
    mutation.mutate(
      {
        username: values.username,
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        phoneNumber: values.phoneNumber || undefined,
      },
      {
        onSuccess: (user) => navigation.replace('UserDetail', { id: user.id }),
      },
    );
  }

  return (
    <ScrollView>
      <UserForm
        submitLabel="Create User"
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        onSubmit={handleSubmit}
      />
    </ScrollView>
  );
}
