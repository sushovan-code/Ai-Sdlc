const DEFAULT_URL = '/';

/** Accepts only in-app paths so a crafted `returnUrl` cannot send the user to another site after sign-in. */
export function safeReturnUrl(candidate: string | null | undefined): string {
  const isInAppPath = !!candidate && candidate.startsWith('/') && !candidate.startsWith('//');
  return isInAppPath && !candidate.includes('\\') ? candidate : DEFAULT_URL;
}
