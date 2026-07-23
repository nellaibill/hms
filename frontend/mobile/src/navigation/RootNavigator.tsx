import { NavigationContainer } from '@react-navigation/native';
import { UsersNavigator } from './UsersNavigator';

/**
 * Only the Users module exists so far, so the root navigator renders it directly.
 * Once Authentication ships, this becomes an Auth/App navigator switch keyed off
 * session state, mirroring the web app's public/protected route split
 * (docs/FrontendArchitecture.md §4).
 */
export function RootNavigator() {
  return (
    <NavigationContainer>
      <UsersNavigator />
    </NavigationContainer>
  );
}
