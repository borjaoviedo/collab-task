export const toast = {
  error(msg: string) {
    // Replace with your real UI later
    console.warn("[toast:error]", msg)
    // eslint-disable-next-line no-alert
    alert(msg)
  },
  success(msg: string) {
    console.info("[toast:success]", msg)
    // eslint-disable-next-line no-alert
    alert(msg)
  },
}