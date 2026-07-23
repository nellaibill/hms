import { Link, useParams } from 'react-router-dom';
import { UserDetails, useUserQuery } from '../../features/users';

export default function UserViewPage() {
  const { id } = useParams<{ id: string }>();
  const { data: user, isPending, isError } = useUserQuery(id);

  if (isPending) {
    return <p>Loading user…</p>;
  }

  if (isError || !user) {
    return <p className="form-error-banner">User not found.</p>;
  }

  return (
    <section>
      <h1>
        {user.firstName} {user.lastName}
      </h1>
      <UserDetails user={user} />
      <Link to={`/users/${user.id}/edit`}>Edit</Link>
    </section>
  );
}
