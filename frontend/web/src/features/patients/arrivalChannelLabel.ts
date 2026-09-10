import { OFFLINE_AD_CHANNELS, ONLINE_AD_CHANNELS, type OfflineAdChannel, type OnlineAdChannel } from '@hms/shared';

/** Explicit overrides for the two Advertisement-channel enums — generic humanize() reads
 * PascalCase by inserting a space before every capital letter, which mangles acronyms/brand
 * names that already have internal caps: "WhatsApp" -> "Whats App", "TvNews" -> "Tv News",
 * "FmAd" -> "Fm Ad". Everything else here happens to already read fine split on capitals, but
 * is spelled out anyway so this stays one single source of truth per enum instead of "most
 * values via humanize, a few exceptions layered on top". */
const ONLINE_AD_CHANNEL_LABELS: Record<OnlineAdChannel, string> = {
  Google: 'Google',
  HospitalWebsite: 'Hospital Website',
  Facebook: 'Facebook',
  Instagram: 'Instagram',
  WhatsApp: 'WhatsApp',
  Other: 'Other',
};

const OFFLINE_AD_CHANNEL_LABELS: Record<OfflineAdChannel, string> = {
  Buses: 'Buses',
  Theatres: 'Theatres',
  Banners: 'Banners',
  Barricades: 'Barricades',
  RoadsideDisplays: 'Roadside Displays',
  OutsideNameBoards: 'Outside Name Boards',
  Pamphlets: 'Pamphlets',
  TvNews: 'TV News',
  FmAd: 'FM Ad',
  Newspapers: 'Newspapers',
  HealthCamps: 'Health Camps',
  AwarenessPrograms: 'Awareness Programs',
  Other: 'Other',
};

export function onlineAdChannelLabel(channel: OnlineAdChannel): string {
  return ONLINE_AD_CHANNEL_LABELS[channel];
}

export function offlineAdChannelLabel(channel: OfflineAdChannel): string {
  return OFFLINE_AD_CHANNEL_LABELS[channel];
}

/** For read-only display (PatientDetails.tsx's Registration Details card) where the stored
 * channel's category isn't known at the call site — it could be an Online/Offline Ad channel
 * token, a Patient/Relative Referral source token ("SameDepartment"/"Other", which already
 * reads fine either way), or free text (a Doctor Referral's department name). Checks the known
 * token sets first and falls back to the raw value unchanged — never runs free text through
 * humanize(), which would be as likely to mangle a department name as to helpfully split it. */
export function modeOfArrivalChannelLabel(channel: string): string {
  if ((ONLINE_AD_CHANNELS as readonly string[]).includes(channel)) {
    return onlineAdChannelLabel(channel as OnlineAdChannel);
  }
  if ((OFFLINE_AD_CHANNELS as readonly string[]).includes(channel)) {
    return offlineAdChannelLabel(channel as OfflineAdChannel);
  }
  if (channel === 'SameDepartment') {
    return 'Same Department';
  }
  return channel;
}
