import { createApp } from '../server.js';
import { isAbsolute } from 'node:path';

const host='127.0.0.1';
const defaultPort=32187;
const channel=process.parentPort;
let site, stopping=false;

async function shutdown(code=0){
  if(stopping)return;
  stopping=true;
  try{await site?.close();}
  catch(error){console.error(error);code=1;}
  process.exit(code);
}

function listen(port){
  const {server}=site;
  return new Promise((resolve,reject)=>{
    const failed=error=>{
      server.off('listening',ready);
      reject(error);
    };
    const ready=()=>{
      server.off('error',failed);
      resolve(server.address().port);
    };
    server.once('error',failed);
    server.once('listening',ready);
    server.listen(port,host);
  });
}

channel.on('message',({data})=>{
  if(data==='shutdown')void shutdown();
});

try{
  const database=process.argv[2];
  if(!database || !isAbsolute(database))throw new Error('Caminho do banco local inválido.');
  site=await createApp({db:{filename:database,url:''}});
  // Reuse the origin so Chromium keeps the local settings between launches.
  const port=await listen(defaultPort).catch(error=>{
    if(error.code!=='EADDRINUSE')throw error;
    return listen(0);
  });
  process.env.PUBLIC_ORIGIN=`http://${host}:${port}`;
  channel.postMessage({type:'ready',port});
}catch(error){
  channel.postMessage({type:'error',message:error.message});
  await shutdown(1);
}
