"""Compatibility entry point for the updated Sprint 1 and accounts suite.

The old Sprint 1-only assertions (including Sprint 2 APIs returning 404) have
been replaced. Set both HR and Admin test passwords as described in README.md.
"""
from sprint1_accounts_smoke import Suite

if __name__ == "__main__":
    Suite().run()
