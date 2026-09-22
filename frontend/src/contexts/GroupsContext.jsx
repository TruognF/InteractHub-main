import { createContext, useContext, useState, useEffect } from 'react';
import { getGroups, joinGroupApi, leaveGroupApi } from '../api';

const GroupsContext = createContext();

export const useGroups = () => {
  const context = useContext(GroupsContext);
  if (!context) {
    throw new Error('useGroups must be used within a GroupsProvider');
  }
  return context;
};

export const GroupsProvider = ({ children }) => {
  const [groups, setGroups] = useState([]);
  const [groupsLoading, setGroupsLoading] = useState(true);

  const refreshGroups = async () => {
    const token = localStorage.getItem('token');
    if (!token) {
      setGroups([]);
      setGroupsLoading(false);
      return;
    }

    setGroupsLoading(true);
    try {
      const fetchedGroups = await getGroups();
      setGroups(fetchedGroups);
    } catch (err) {
      console.error('Error loading groups from backend:', err);
      setGroups([]);
    } finally {
      setGroupsLoading(false);
    }
  };

  useEffect(() => {
    refreshGroups();

    import('../utils/groupHubConnection').then(({ startConnection }) => {
      startConnection();
    });

    const handleGroupCreated = () => {
      refreshGroups();
    };

    const handleGroupUpdated = () => {
      refreshGroups();
    };

    const handleTokenChange = () => {
      refreshGroups();
    };

    window.addEventListener("signalr:group-created", handleGroupCreated);
    window.addEventListener("signalr:group-updated", handleGroupUpdated);
    window.addEventListener('tokenUpdated', handleTokenChange);
    window.addEventListener('storage', handleTokenChange);

    return () => {
      window.removeEventListener("signalr:group-created", handleGroupCreated);
      window.removeEventListener("signalr:group-updated", handleGroupUpdated);
      window.removeEventListener('tokenUpdated', handleTokenChange);
      window.removeEventListener('storage', handleTokenChange);
    };
  }, []);

  const joinGroup = async (groupId) => {
    try {
      await joinGroupApi(groupId);
      setGroups(prevGroups =>
        prevGroups.map(g =>
          g.id === groupId ? { ...g, isJoined: true } : g
        )
      );
    } catch (err) {
      console.error('Error joining group:', err);
    }
  };

  const leaveGroup = async (groupId) => {
    try {
      await leaveGroupApi(groupId);
      setGroups(prevGroups =>
        prevGroups.map(g =>
          g.id === groupId ? { ...g, isJoined: false } : g
        )
      );
    } catch (err) {
      console.error('Error leaving group:', err);
    }
  };

  return (
    <GroupsContext.Provider value={{ groups, groupsLoading, joinGroup, leaveGroup, refreshGroups }}>
      {children}
    </GroupsContext.Provider>
  );
};
