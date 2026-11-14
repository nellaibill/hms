import type { Address } from '@hms/shared';
import { Fragment, type ReactNode } from 'react';
import { DistrictName } from '@/components/DistrictName';
import { StateName } from '@/components/StateName';

/**
 * "District, State — Pincode" for a patient's address, leaving out whatever wasn't recorded —
 * State, District and Pincode can all be blank where the hospital makes them optional
 * (ADR-135). Renders nothing when none of them is there.
 */
export function AddressPlace({ address }: { address: Address }) {
  const places: ReactNode[] = [];
  if (address.stateId && address.districtId) {
    places.push(
      <DistrictName key="district" stateId={address.stateId} districtId={address.districtId} />,
    );
  }
  if (address.stateId) {
    places.push(<StateName key="state" stateId={address.stateId} />);
  }

  if (places.length === 0 && !address.pincode) return null;

  return (
    <>
      {places.map((place, index) => (
        <Fragment key={index}>
          {index > 0 && ', '}
          {place}
        </Fragment>
      ))}
      {address.pincode && (places.length > 0 ? ` — ${address.pincode}` : address.pincode)}
    </>
  );
}
