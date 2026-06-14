import '@testing-library/jest-dom'

// jsdom does not implement scrollTo — stub it globally
window.scrollTo = () => { }
