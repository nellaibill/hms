import { StyleSheet, Text, View } from 'react-native';

interface StatusBadgeProps {
  isActive: boolean;
}

export function StatusBadge({ isActive }: StatusBadgeProps) {
  return (
    <View style={[styles.badge, isActive ? styles.active : styles.inactive]}>
      <Text style={[styles.text, isActive ? styles.activeText : styles.inactiveText]}>
        {isActive ? 'Active' : 'Inactive'}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: { alignSelf: 'flex-start', paddingHorizontal: 8, paddingVertical: 2, borderRadius: 999 },
  active: { backgroundColor: '#e3f6e8' },
  inactive: { backgroundColor: '#f0f0f0' },
  text: { fontSize: 12 },
  activeText: { color: '#1e7a34' },
  inactiveText: { color: '#666' },
});
