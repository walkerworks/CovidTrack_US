import axios from 'axios';

interface CovidTrackUser {
  Id: string;
  handle: string,
  confirmed: boolean;
  counties: {
      state: string;
      county: string;
    }[];
}

let covidTrackUser : CovidTrackUser;
export function getLocalUser() : CovidTrackUser {
  if (typeof covidTrackUser === 'object') return covidTrackUser;

  const storedUser = localStorage.getItem('covidTrackUser');
  if (storedUser) {
    try {
      covidTrackUser = JSON.parse(storedUser);
    }
    catch (err) {
      console.warn(`error parsing ${storedUser}`, err);
      covidTrackUser = null;
    }
    return covidTrackUser;
  }

  const windowUser = window['covidTrackUser'];
  if (windowUser) {
    let stringifiedUser = JSON.stringify(windowUser);
    localStorage.setItem('covidTrackUser', stringifiedUser);
    covidTrackUser = JSON.parse(stringifiedUser);
  }
  else {
    covidTrackUser = null;
  }

  return covidTrackUser;
}

export async function logout() {
  const authUrl = '/api/logout';
  await axios.post(authUrl,{ withCredentials: true });
  window['covidTrackUser'] = null;
  localStorage.removeItem('covidTrackUser');
  covidTrackUser = null;
  this.$eventHub.$emit('logout');
  this.$router.push({name: 'Home'});
}