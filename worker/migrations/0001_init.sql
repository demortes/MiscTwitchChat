CREATE TABLE settings (
  channel TEXT NOT NULL,
  name    TEXT NOT NULL,
  value   TEXT,
  PRIMARY KEY (channel, name)
);

CREATE TABLE disconsenters (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  name    TEXT,
  created TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE INDEX idx_disconsenters_name ON disconsenters (name);

CREATE TABLE command_counts (
  channel      TEXT NOT NULL,
  target_user  TEXT NOT NULL,
  command_used TEXT NOT NULL,
  count        INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (channel, command_used, target_user)
);

-- Timestamps are ISO-8601 UTC text, so they sort and compare lexicographically.
CREATE TABLE active_chatters (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  username   TEXT NOT NULL,
  channel    TEXT NOT NULL,
  last_seen  TEXT NOT NULL,
  first_seen TEXT NOT NULL,
  UNIQUE (channel, username)
);
CREATE INDEX idx_active_chatters_channel_last_seen ON active_chatters (channel, last_seen);
