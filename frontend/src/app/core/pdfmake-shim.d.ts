declare module 'pdfmake/build/pdfmake' {
  const pdfMake: unknown;
  export default pdfMake;
}

declare module 'pdfmake/build/vfs_fonts' {
  const fonts: Record<string, string>;
  export default fonts;
}
