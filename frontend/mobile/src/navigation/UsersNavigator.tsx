import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { UserCreateScreen } from '../screens/users/UserCreateScreen';
import { UserDetailScreen } from '../screens/users/UserDetailScreen';
import { UserEditScreen } from '../screens/users/UserEditScreen';
import { UsersListScreen } from '../screens/users/UsersListScreen';
import type { UsersStackParamList } from './types';

const Stack = createNativeStackNavigator<UsersStackParamList>();

export function UsersNavigator() {
  return (
    <Stack.Navigator initialRouteName="UsersList">
      <Stack.Screen name="UsersList" component={UsersListScreen} options={{ title: 'Users' }} />
      <Stack.Screen name="UserDetail" component={UserDetailScreen} options={{ title: 'User' }} />
      <Stack.Screen name="UserCreate" component={UserCreateScreen} options={{ title: 'New User' }} />
      <Stack.Screen name="UserEdit" component={UserEditScreen} options={{ title: 'Edit User' }} />
    </Stack.Navigator>
  );
}
