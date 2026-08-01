import { ApiError, userProfileSchema, type UserProfileFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRolesForSelect } from '../hooks/useRolesForSelect';

interface UserFormProps {
  defaultValues?: Partial<UserProfileFormValues>;
  onSubmit: (values: UserProfileFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function UserForm({ defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: UserFormProps) {
  const { data: roles } = useRolesForSelect();

  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<UserProfileFormValues>({
    resolver: zodResolver(userProfileSchema),
    defaultValues: {
      username: '',
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      roleId: '',
      ...defaultValues,
    },
  });

  // Server-side validation failures mapped onto the same field-level display client
  // validation uses (docs/FrontendArchitecture.md §9), mirroring the web implementation.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof UserProfileFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <View style={styles.form}>
      {generalError && <Text style={styles.bannerError}>{generalError}</Text>}

      <View style={styles.field}>
        <Text style={styles.label}>Username</Text>
        <Controller
          control={control}
          name="username"
          render={({ field }) => (
            <TextInput
              style={styles.input}
              value={field.value}
              onChangeText={field.onChange}
              onBlur={field.onBlur}
              autoCapitalize="none"
              autoComplete="username"
            />
          )}
        />
        {errors.username && <Text style={styles.fieldError}>{errors.username.message}</Text>}
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>First name</Text>
        <Controller
          control={control}
          name="firstName"
          render={({ field }) => (
            <TextInput
              style={styles.input}
              value={field.value}
              onChangeText={field.onChange}
              onBlur={field.onBlur}
              autoComplete="given-name"
            />
          )}
        />
        {errors.firstName && <Text style={styles.fieldError}>{errors.firstName.message}</Text>}
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Last name</Text>
        <Controller
          control={control}
          name="lastName"
          render={({ field }) => (
            <TextInput
              style={styles.input}
              value={field.value}
              onChangeText={field.onChange}
              onBlur={field.onBlur}
              autoComplete="family-name"
            />
          )}
        />
        {errors.lastName && <Text style={styles.fieldError}>{errors.lastName.message}</Text>}
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Email</Text>
        <Controller
          control={control}
          name="email"
          render={({ field }) => (
            <TextInput
              style={styles.input}
              value={field.value}
              onChangeText={field.onChange}
              onBlur={field.onBlur}
              autoCapitalize="none"
              keyboardType="email-address"
              autoComplete="email"
            />
          )}
        />
        {errors.email && <Text style={styles.fieldError}>{errors.email.message}</Text>}
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Phone number</Text>
        <Controller
          control={control}
          name="phoneNumber"
          render={({ field }) => (
            <TextInput
              style={styles.input}
              value={field.value ?? ''}
              onChangeText={field.onChange}
              onBlur={field.onBlur}
              keyboardType="phone-pad"
              autoComplete="tel"
            />
          )}
        />
        {errors.phoneNumber && <Text style={styles.fieldError}>{errors.phoneNumber.message}</Text>}
      </View>

      <View style={styles.field}>
        <Text style={styles.label}>Role</Text>
        <Controller
          control={control}
          name="roleId"
          render={({ field }) => (
            <View style={styles.roleList}>
              {roles?.items.map((role) => (
                <Pressable
                  key={role.id}
                  style={[styles.roleChip, field.value === role.id && styles.roleChipSelected]}
                  onPress={() => field.onChange(role.id)}
                >
                  <Text style={[styles.roleChipText, field.value === role.id && styles.roleChipTextSelected]}>
                    {role.name}
                  </Text>
                </Pressable>
              ))}
            </View>
          )}
        />
        {errors.roleId && <Text style={styles.fieldError}>{errors.roleId.message}</Text>}
      </View>

      <Pressable
        style={[styles.button, isSubmitting && styles.buttonDisabled]}
        onPress={handleSubmit(onSubmit)}
        disabled={isSubmitting}
      >
        <Text style={styles.buttonText}>{isSubmitting ? 'Saving…' : submitLabel}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  form: { padding: 16 },
  field: { marginBottom: 16 },
  label: { fontSize: 14, marginBottom: 4, color: '#333' },
  input: { borderWidth: 1, borderColor: '#ccc', borderRadius: 6, padding: 10 },
  fieldError: { color: '#b3261e', fontSize: 12, marginTop: 4 },
  roleList: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  roleChip: { borderWidth: 1, borderColor: '#ccc', borderRadius: 16, paddingVertical: 6, paddingHorizontal: 12 },
  roleChipSelected: { backgroundColor: '#1f2a44', borderColor: '#1f2a44' },
  roleChipText: { color: '#333', fontSize: 13 },
  roleChipTextSelected: { color: '#fff' },
  bannerError: { color: '#b3261e', backgroundColor: '#fdecea', padding: 10, borderRadius: 6, marginBottom: 12 },
  button: { backgroundColor: '#1f2a44', padding: 14, borderRadius: 6, alignItems: 'center' },
  buttonDisabled: { opacity: 0.6 },
  buttonText: { color: '#fff', fontWeight: '600' },
});
