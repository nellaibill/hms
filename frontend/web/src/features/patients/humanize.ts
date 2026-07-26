/** "FatherInLaw" -> "Father In Law", "TvNews" -> "Tv News" — for rendering PascalCase enum tokens as select option labels. */
export function humanize(token: string): string {
  return token.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2');
}
