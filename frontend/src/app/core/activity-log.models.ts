export interface ActivityLogQuery {
  dateFrom: string | null;
  dateTo: string | null;
  actor: string | null;
  action: string | null;
  entityType: string | null;
  search: string | null;
}

export interface ActivityLogOptions {
  actors: string[];
  actions: string[];
  entityTypes: string[];
}

function blankActivityLogQuery(): ActivityLogQuery {
  return { dateFrom: null, dateTo: null, actor: null, action: null, entityType: null, search: null };
}

/** Rows keep forever; the screen opens on a week so the list stays readable. */
export const DEFAULT_ACTIVITY_LOG_DAYS = 7;

export function defaultActivityLogQuery(): ActivityLogQuery {
  return { ...blankActivityLogQuery(), ...dayRange(DEFAULT_ACTIVITY_LOG_DAYS) };
}

/** Inclusive day range as yyyy-MM-dd strings, which is what the API's date filters expect. */
export function dayRange(daysBack: number): { dateFrom: string; dateTo: string } {
  const iso = (day: Date) =>
    `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, '0')}-${String(day.getDate()).padStart(2, '0')}`;

  const to = new Date();
  const from = new Date();
  from.setDate(to.getDate() - daysBack);

  return { dateFrom: iso(from), dateTo: iso(to) };
}
