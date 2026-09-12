# Privacy Policy for November

**Last Updated:** 13/09/2026

## 1. Overview

November is a Discord bot designed for utility features, casual interaction, and AI-assisted commentary.  
November does not provide AI companionship services and does not store conversation histories.

This Privacy Policy explains what information is collected and how it is used.

---

## 2. Information Collected

November collects only the minimum data required to operate:

### 2.1 Guild Data
- Discord Guild (Server) IDs  
Used to store server-specific configuration.

### 2.2 User Data
- Discord User IDs (stored only when necessary for a specific feature, such as honeypot moderation logs)

November does not collect or store:
- Message history
- Conversation logs
- User profiles
- Behavioral analytics
- Personal identifying information beyond Discord IDs

### 2.3 Honeypot Feature
- Data Collection Scope: When the honeypot feature is enabled by a server administrator, the bot stores posted message content alongside the author's Discord User ID and username for moderation logging.
- Access Control: This logged data remains exclusively visible to server administrators via the designated mod-log channel.
- Administrator Controls: Server administrators retain the authority to disable the honeypot feature or request data deletion at any time.

---

## 3. Command Processing & AI Services

- Invocation and AI Handling: When you invoke the `/llm` command or use the NASA module, your query or retrieved public NASA data is sent to Google's API for generation, or processed locally depending on the instance configuration.
- Data Retention: November does not store or log your command text or personal inputs after a response is delivered.
- External Privacy: All interactions processed via Google are governed by Google’s data handling policies and privacy terms.

---

## 4. Third-Party Services

November relies on the following third-party services:

- Discord API
- Brave Search API (for /search command queries)
- NASA public APIs
- AI language model providers, which may be third-party services or models running on the developer's own infrastructure, depending on configuration

No data is sold, monetized, or used for advertising purposes.

---

## 5. Data Retention

- Guild IDs and User IDs are stored only as long as necessary for bot functionality.
- Users may request deletion of stored User ID data.
- Server administrators may remove November at any time, which will prevent further data processing within that server.

---

## 6. Data Security

November is designed with a minimal data retention philosophy.  
Only essential identifiers are stored. Message content is not persisted, except within the honeypot feature, which stores the content of messages posted in a server administrator's designated honeypot channel for the purpose of moderation logging.

---

## 7. Contact

For data deletion requests or privacy inquiries, contact ribs351 on Discord.
